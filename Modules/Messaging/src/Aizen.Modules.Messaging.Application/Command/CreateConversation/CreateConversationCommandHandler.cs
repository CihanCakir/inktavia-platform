using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface.Repository;

namespace Aizen.Modules.Messaging.Application.Command.CreateConversation;

[DocumentationInfo("Create conversation command handler",
    "Creates a new conversation or returns the existing one for the given context (idempotent).")]
public sealed class CreateConversationCommandHandler
    : AizenCommandHandler<CreateConversationCommand, CreateConversationResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly MessagingRealtimePublisher _realtimePublisher;

    public CreateConversationCommandHandler(
        IConversationRepository conversationRepository,
        MessagingRealtimePublisher realtimePublisher)
    {
        _conversationRepository = conversationRepository;
        _realtimePublisher      = realtimePublisher;
    }

    public override async Task<CreateConversationResponse?> Handle(
        CreateConversationCommand request, CancellationToken cancellationToken)
    {
        // Idempotency: return existing conversation if already exists for this context
        var existing = await _conversationRepository.GetByContextAsync(
            request.ContextType, request.ContextId, cancellationToken);

        if (existing is not null)
            return new CreateConversationResponse(existing.Id.ToString());

        var entity = ConversationEntity.Create(request.ContextType, request.ContextId, request.Title);

        await _conversationRepository.AddAsync(entity, cancellationToken);

        foreach (var p in request.Participants)
        {
            var participant = ConversationParticipantEntity.Create(
                entity.Id, p.UserId, p.DisplayName, p.Role);
            entity.AddParticipant(participant);
        }

        _conversationRepository.Update(entity);

        await _realtimePublisher.PublishConversationStatusChangedAsync(
            entity.Id, "Created", cancellationToken);

        return new CreateConversationResponse(entity.Id.ToString());
    }
}
