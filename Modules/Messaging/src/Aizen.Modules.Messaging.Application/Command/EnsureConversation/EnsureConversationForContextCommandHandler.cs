using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface.Repository;

namespace Aizen.Modules.Messaging.Application.Command.EnsureConversation;

[DocumentationInfo("Ensure conversation for context command handler",
    "Idempotent get-or-create keyed on (ContextType, ContextId). Returns the existing conversation untouched if " +
    "present (participants are NOT re-added); otherwise creates it with the supplied participants.")]
public sealed class EnsureConversationForContextCommandHandler
    : AizenCommandHandler<EnsureConversationForContextCommand, EnsureConversationResponse>
{
    private readonly IConversationRepository _conversationRepository;

    public EnsureConversationForContextCommandHandler(IConversationRepository conversationRepository)
        => _conversationRepository = conversationRepository;

    public override async Task<EnsureConversationResponse?> Handle(
        EnsureConversationForContextCommand request, CancellationToken cancellationToken)
    {
        // Idempotency: one conversation per (ContextType, ContextId) — DB-enforced by the unique index.
        var existing = await _conversationRepository.GetByContextAsync(
            request.ContextType, request.ContextId, cancellationToken);

        if (existing is not null)
            return new EnsureConversationResponse(existing.Id, Created: false);

        var entity = ConversationEntity.Create(request.ContextType, request.ContextId, request.Title);
        await _conversationRepository.AddAsync(entity, cancellationToken);

        foreach (var p in request.Participants)
            entity.AddParticipant(
                ConversationParticipantEntity.Create(entity.Id, p.UserId, p.DisplayName, p.Role));

        _conversationRepository.Update(entity);

        return new EnsureConversationResponse(entity.Id, Created: true);
    }
}
