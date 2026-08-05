using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Command.CreateSupportRequest;

/// <summary>
/// N-D — opens (or reuses) a live-support conversation for the authenticated requester.
/// The requester id is resolved server-side (not trusted from the client). ContextId is derived deterministically from
/// (requester, topic) so a user reuses one thread per topic and separate topics land in separate conversations.
/// </summary>
public sealed class CreateSupportRequestCommand : AizenCommand<CreateSupportRequestResponse>
{
    public SupportTopic Topic                { get; init; }
    public string       Subject              { get; init; } = default!;
    public string?      FirstMessage         { get; init; }
    /// <summary>Display name for the requester participant (resolved by the BFF, e.g. the provider's company name).</summary>
    public string       RequesterDisplayName { get; init; } = "Kullanıcı";
    /// <summary>Participant role for the requester (BFF passes Provider for the provider flow; Owner otherwise).</summary>
    public MessagingParticipantRole RequesterRole { get; init; } = MessagingParticipantRole.Owner;
}
