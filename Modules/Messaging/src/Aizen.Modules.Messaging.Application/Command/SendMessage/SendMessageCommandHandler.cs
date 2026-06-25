using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;

namespace Aizen.Modules.Messaging.Application.Command.SendMessage;

[DocumentationInfo("Send message command handler",
    "Validates content policy, persists the message, and broadcasts realtime events.")]
public sealed class SendMessageCommandHandler
    : AizenCommandHandler<SendMessageCommand, SendMessageResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationMessageRepository _messageRepository;
    private readonly IMessageContentPolicy _contentPolicy;
    private readonly MessagingRealtimePublisher _realtimePublisher;
    private readonly IAizenInfoAccessor _info;

    public SendMessageCommandHandler(
        IConversationRepository conversationRepository,
        IConversationMessageRepository messageRepository,
        IMessageContentPolicy contentPolicy,
        MessagingRealtimePublisher realtimePublisher,
        IAizenInfoAccessor info)
    {
        _conversationRepository = conversationRepository;
        _messageRepository      = messageRepository;
        _contentPolicy          = contentPolicy;
        _realtimePublisher      = realtimePublisher;
        _info                   = info;
    }

    public override async Task<SendMessageResponse?> Handle(
        SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {request.ConversationId} not found.");

        if (conversation.Status == ConversationStatus.Closed)
            throw new InvalidOperationException("Cannot send message to a closed conversation.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var participant   = conversation.Participants.FirstOrDefault(p => p.UserId == currentUserId)
            ?? throw new UnauthorizedAccessException("Sender is not a participant of this conversation.");

        // Content moderation
        var policyResult = await _contentPolicy.EvaluateAsync(request.Content, currentUserId, cancellationToken);

        if (!policyResult.IsAllowed)
        {
            await _realtimePublisher.PublishModerationEventAsync(
                conversation.Id, 0, policyResult.PolicyCode!, policyResult.ViolationReason!, cancellationToken);
            throw new InvalidOperationException(
                $"Message blocked by content policy: {policyResult.ViolationReason}");
        }

        var message = ConversationMessageEntity.Create(
            conversation.Id, currentUserId, participant.DisplayName,
            participant.Role, request.Content, request.Type, request.IsInternalNote);

        if (policyResult.RequiresReview)
            message.Flag(policyResult.ViolationReason!);

        if (!string.IsNullOrEmpty(request.AttachmentFileStorageId))
        {
            var attachment = MessageAttachmentEntity.Create(
                0,
                request.AttachmentFileName ?? "attachment",
                request.AttachmentFileType ?? "document",
                request.AttachmentFileStorageId);
            message.AddAttachment(attachment);
        }

        await _messageRepository.AddAsync(message, cancellationToken);
        conversation.AddMessage(message);
        _conversationRepository.Update(conversation);

        if (policyResult.RequiresReview)
            await _realtimePublisher.PublishModerationEventAsync(
                conversation.Id, message.Id, "REVIEW_REQUIRED", policyResult.ViolationReason!, cancellationToken);

        var participantIds = conversation.Participants.Select(p => p.UserId);
        await _realtimePublisher.PublishMessageSentAsync(
            conversation.Id, conversation.ContextId, conversation.ContextType,
            message.ToDto(), participantIds, cancellationToken);

        return new SendMessageResponse(message.ToDto());
    }
}
