using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Modules.Payment.Application.Commands.ResolveDisputeOutcome;

/// <summary>
/// BE-S13b — drives the P10 refund/escrow path for a resolved dispute. Wraps the remote-call request; returns the
/// remote-call response. Reuses <c>RefundAllocationService</c> (§7.5 nine-field allocation) + the gateway release —
/// no bespoke refund math. Idempotent on the dispute context ref <c>DISPUTE-{DisputeId}</c>.
/// </summary>
public sealed class ResolveDisputeOutcomeCommand : AizenCommand<ResolveDisputeOutcomeRemoteCallResponse>
{
    public required ResolveDisputeOutcomeRemoteCallRequest Request { get; init; }
}
