namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

/// <summary>
/// Internal dev/ops request: force the CargoDry-supply accept-timeout fallback for a single SR without waiting for the
/// hourly sweep. Dispatches FallbackCargoDrySupplyToCargoCommand (which itself re-guards status ∈ {Open, WaitingForOffer,
/// OfferReceived}) — the deadline is NOT re-checked here, so it works on any still-open supply order.
/// </summary>
public sealed class ForceCargoDryAcceptTimeoutRequest
{
    public long ServiceRequestId { get; init; }
}
