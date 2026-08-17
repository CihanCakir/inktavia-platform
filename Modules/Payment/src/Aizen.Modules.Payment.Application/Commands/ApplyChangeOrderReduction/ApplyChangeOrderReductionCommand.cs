using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Modules.Payment.Application.Commands.ApplyChangeOrderReduction;

/// <summary>
/// BE-S11b — applies a change-order reduction by refunding the delta against the SR's original escrow. Wraps the
/// remote-call request; returns the remote-call response. Reuses the gateway refund + <c>RefundAllocationService</c>
/// (§7.5 nine-field allocation) — no bespoke refund math, no snapshot mutation. Idempotent on
/// <c>SR-{sr}-OFFER-{offer}-CO-{id}</c>.
/// </summary>
public sealed class ApplyChangeOrderReductionCommand : AizenCommand<ApplyChangeOrderReductionRemoteCallResponse>
{
    public required ApplyChangeOrderReductionRemoteCallRequest Request { get; init; }
}
