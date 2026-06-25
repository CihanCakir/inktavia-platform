using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Messaging.Application.Command.SendMessage;

[DocumentationInfo("Send message command handler",
    "Validates content policy, persists the message, completes file uploads, fires LLM analysis, and broadcasts realtime events.")]
public sealed class SendMessageCommandHandler
    : AizenCommandHandler<SendMessageCommand, SendMessageResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationMessageRepository _messageRepository;
    private readonly IMessageContentPolicy _contentPolicy;
    private readonly IMessagingFileStorageService _fileStorage;
    private readonly MessagingRealtimePublisher _realtimePublisher;
    private readonly IAizenInfoAccessor _info;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IConversationRepository conversationRepository,
        IConversationMessageRepository messageRepository,
        IMessageContentPolicy contentPolicy,
        IMessagingFileStorageService fileStorage,
        MessagingRealtimePublisher realtimePublisher,
        IAizenInfoAccessor info,
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<SendMessageCommandHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _messageRepository      = messageRepository;
        _contentPolicy          = contentPolicy;
        _fileStorage            = fileStorage;
        _realtimePublisher      = realtimePublisher;
        _info                   = info;
        _serviceProvider        = serviceProvider;
        _config                 = config;
        _logger                 = logger;
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

        // Content moderation — Location type skips text checks
        var policyResult = await _contentPolicy.EvaluateAsync(
            request.Content, currentUserId, request.Type, cancellationToken);

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

        // Attachment handling — legacy direct FileStorageId path
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

        // Complete upload session if UploadSessionCode provided (new flow)
        if (request.Type == MessageType.MediaAttachment
            && !string.IsNullOrWhiteSpace(request.UploadSessionCode))
        {
            try
            {
                var fileId = await _fileStorage.CompleteUploadSessionAsync(
                    request.UploadSessionCode, request.Checksum, cancellationToken);

                if (fileId.HasValue && message.Attachments.Any())
                    message.Attachments.First().SetFileStorageId(fileId.Value.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to complete upload session for message {MessageId}.", message.Id);
            }
        }

        if (policyResult.RequiresReview)
            await _realtimePublisher.PublishModerationEventAsync(
                conversation.Id, message.Id, "REVIEW_REQUIRED", policyResult.ViolationReason!, cancellationToken);

        var participantIds = conversation.Participants.Select(p => p.UserId);
        await _realtimePublisher.PublishMessageSentAsync(
            conversation.Id, conversation.ContextId, conversation.ContextType,
            message.ToDto(), participantIds, cancellationToken);

        // Fire-and-forget LLM analysis — only for text messages, non-blocking
        var llmEnabled = _config.GetValue<bool>("Messaging:LlmModeration:Enabled", defaultValue: false);
        if (llmEnabled && request.Type == MessageType.Text && !request.IsInternalNote)
        {
            var savedMessageId  = message.Id;
            var savedContent    = request.Content;
            var savedConvId     = conversation.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope  = _serviceProvider.CreateScope();
                    var analyzer     = scope.ServiceProvider.GetRequiredService<ILlmContentAnalyzer>();
                    var db           = scope.ServiceProvider.GetRequiredService<Aizen.Modules.Messaging.Repository.Persistence.MessagingDbContext>();
                    await analyzer.AnalyzeAsync(savedMessageId, savedContent, savedConvId);
                    await db.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Background LLM analysis scope failed for message {MessageId}.", message.Id);
                }
            }, CancellationToken.None);
        }

        return new SendMessageResponse(message.ToDto());
    }
}
