using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Application.Command.CreateConversation;

namespace Aizen.Modules.Messaging.Application.Command.EnsureConversation;

[DocumentationInfo("Ensure conversation for context command",
    "Idempotently returns the existing Messaging conversation for (ContextType, ContextId) or creates it with the " +
    "given participants. The linchpin for the ServiceRequest→Messaging unification: an SR gets its canonical " +
    "Messaging conversation on first message / at SR creation (P2). Unique on (ContextType, ContextId).")]
public sealed class EnsureConversationForContextCommand : AizenCommand<EnsureConversationResponse>
{
    public MessagingContextType ContextType                { get; }
    public long ContextId                                  { get; }
    public string Title                                    { get; }
    public List<ConversationParticipantInput> Participants { get; }

    public EnsureConversationForContextCommand(
        MessagingContextType contextType,
        long contextId,
        string title,
        List<ConversationParticipantInput> participants)
    {
        ContextType  = contextType;
        ContextId    = contextId;
        Title        = title;
        Participants = participants;
    }
}

/// <summary>Result of an ensure: the (existing or created) conversation id, and whether it was created this call.</summary>
public sealed record EnsureConversationResponse(long ConversationId, bool Created);
