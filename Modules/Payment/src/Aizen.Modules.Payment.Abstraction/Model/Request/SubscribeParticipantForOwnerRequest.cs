namespace Aizen.Modules.Payment.Abstraction.Model.Request;

/// <summary>
/// BE-MO7 — owner subscribe payload. Carries ONLY the chosen plan id. The participant identity comes from the token
/// (BFF assertion) and the price is resolved from the plan server-side — a client-supplied price/participant is never
/// accepted.
/// </summary>
public sealed class SubscribeParticipantForOwnerRequest
{
    public required long PlanId { get; init; }
}
