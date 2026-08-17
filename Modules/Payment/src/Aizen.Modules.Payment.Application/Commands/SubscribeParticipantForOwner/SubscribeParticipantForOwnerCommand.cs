using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.SubscribeParticipantForOwner;

/// <summary>
/// BE-MO7 — an owner subscribes themselves to a participant plan. The participant identity comes from the trusted
/// context (BFF assertion), never the body; the price is resolved from the plan server-side, never client-supplied.
/// A FREE/launch plan is applied immediately; a PAID plan initiates an iyzico-gated checkout (the subscription is
/// created by the existing capture consumer). Reuses the plan pricing (P4) + the capture→consumer flow unchanged.
/// </summary>
public sealed class SubscribeParticipantForOwnerCommand : AizenCommand<SubscribeParticipantForOwnerResult>
{
    public required long ParticipantProfileId { get; init; }
    public required long ParticipantPlanId    { get; init; }
}
