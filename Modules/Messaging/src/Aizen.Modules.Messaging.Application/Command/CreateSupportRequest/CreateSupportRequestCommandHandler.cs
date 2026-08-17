using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Message;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Persistence;

namespace Aizen.Modules.Messaging.Application.Command.CreateSupportRequest;

[DocumentationInfo("Create support request command handler",
    "Opens or reuses a live-support conversation for the requester, optionally posts the first message, and emits a SupportRequestOpened event for the admin notification.")]
public sealed class CreateSupportRequestCommandHandler
    : AizenCommandHandler<CreateSupportRequestCommand, CreateSupportRequestResponse>
{
    private readonly IConversationRepository        _conversationRepository;
    private readonly IConversationMessageRepository _messageRepository;
    private readonly MessagingRealtimePublisher     _realtimePublisher;
    private readonly IAizenMessagePublisher         _publisher;
    private readonly IAizenInfoAccessor             _info;
    private readonly MessagingDbContext             _db;

    public CreateSupportRequestCommandHandler(
        IConversationRepository conversationRepository,
        IConversationMessageRepository messageRepository,
        MessagingRealtimePublisher realtimePublisher,
        IAizenMessagePublisher publisher,
        IAizenInfoAccessor info,
        MessagingDbContext db)
    {
        _conversationRepository = conversationRepository;
        _messageRepository      = messageRepository;
        _realtimePublisher      = realtimePublisher;
        _publisher              = publisher;
        _info                   = info;
        _db                     = db;
    }

    public override async Task<CreateSupportRequestResponse?> Handle(
        CreateSupportRequestCommand request, CancellationToken ct)
    {
        var requesterUserId = _info.UserInfoAccessor.UserInfo.UserId;
        if (requesterUserId <= 0)
            throw new UnauthorizedAccessException("Cannot resolve the requester identity.");

        // One reusable support thread per (requester, topic); different topics → separate conversations (own group).
        var contextId = requesterUserId * 100 + (int)request.Topic;

        var existing = await _conversationRepository.GetByContextAsync(
            MessagingContextType.Support, contextId, ct);

        if (existing is not null)
        {
            // Reuse the open thread. Post the new opening message if one was supplied.
            if (!string.IsNullOrWhiteSpace(request.FirstMessage))
            {
                await AppendMessageAsync(existing, requesterUserId, request, ct);
                _conversationRepository.Update(existing);
                await _db.SaveChangesAsync(ct);
            }
            return new CreateSupportRequestResponse(existing.Id.ToString(), false, request.Topic.ToString());
        }

        var entity = ConversationEntity.Create(
            MessagingContextType.Support, contextId, request.Subject, request.Topic);
        await _conversationRepository.AddAsync(entity, ct);

        // Add participant + first message via the navigation collections only. The conversation is in the Added
        // state (temporary Id), so EF cascades the inserts and fixes up the FKs on SaveChanges — do NOT call
        // Update() (that would try to mark the temp-keyed entity Modified) or add the message via its own repository.
        entity.AddParticipant(ConversationParticipantEntity.Create(
            0, requesterUserId, request.RequesterDisplayName, request.RequesterRole));

        if (!string.IsNullOrWhiteSpace(request.FirstMessage))
            entity.AddMessage(ConversationMessageEntity.Create(
                0, requesterUserId, request.RequesterDisplayName,
                request.RequesterRole, request.FirstMessage!, MessageType.Text, isInternalNote: false));

        // Persist now so entity.Id (identity) is assigned before we broadcast / publish it.
        await _db.SaveChangesAsync(ct);

        await _realtimePublisher.PublishConversationStatusChangedAsync(entity.Id, "Created", ct);

        // D4 — alert admins that a new support request opened (dedicated event; consumed by the Notification module).
        await _publisher.PublishAsync(new SupportRequestOpenedMessage
        {
            ConversationId  = entity.Id,
            RequesterUserId = requesterUserId,
            RequesterName   = request.RequesterDisplayName,
            Topic           = request.Topic,
            Subject         = request.Subject,
        }, ct);

        return new CreateSupportRequestResponse(entity.Id.ToString(), true, request.Topic.ToString());
    }

    private async Task AppendMessageAsync(
        ConversationEntity conversation, long senderUserId, CreateSupportRequestCommand request, CancellationToken ct)
    {
        var message = ConversationMessageEntity.Create(
            conversation.Id, senderUserId, request.RequesterDisplayName,
            request.RequesterRole, request.FirstMessage!, MessageType.Text, isInternalNote: false);
        await _messageRepository.AddAsync(message, ct);
        conversation.AddMessage(message);
    }
}
