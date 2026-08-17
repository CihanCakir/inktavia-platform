using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Command.EnsureConversation;

/// <summary>
/// BE_WC4a — idempotent get-or-create of the canonical Messaging conversation for a ServiceRequest, with the owner +
/// accepted-provider participants resolved <b>server-side</b> from the SR schema (mirroring the sync consumer /
/// lifecycle writer). Lets a BFF send handler create the conversation natively on the Messaging side before the very
/// first message, so a fresh SR no longer depends on the SR write to bootstrap it (WC4b then removes that fallback).
/// </summary>
public sealed class EnsureServiceRequestConversationCommand : AizenCommand<EnsureConversationByContextResponse>
{
    public long ServiceRequestId { get; }

    public EnsureServiceRequestConversationCommand(long serviceRequestId)
        => ServiceRequestId = serviceRequestId;
}
