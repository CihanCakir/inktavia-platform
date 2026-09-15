using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.CompleteCargoDrySupplyOrder;

/// <summary>
/// CargoDry supply v2 — completes a CARGODRY_SUPPLY order IN-PROCESS (no user token) from a timed job or an admin
/// action: marks the SR completed/closed and publishes (a) ServiceRequestCompletedMessage → Payment releases the
/// platform-collected escrow in-process, and (b) CargoDrySupplyOrderCompletedMessage → CargoDry records the retail
/// sale (provider attribution+commission for the delivered kit, or a cargo direct-sale). Idempotent: a re-run on an
/// already Completed/Closed/Cancelled order is a no-op.
/// </summary>
public sealed class CompleteCargoDrySupplyOrderCommand : AizenCommand<CompleteCargoDrySupplyOrderResponse>
{
    public long   ServiceRequestId { get; init; }
    /// <summary>Audit note (e.g. "delivered auto-complete", "cargo auto-complete", "admin manual complete").</summary>
    public string? Note            { get; init; }
}

public sealed class CompleteCargoDrySupplyOrderResponse
{
    public bool   Completed        { get; init; }
    public long   ServiceRequestId { get; init; }
    public bool   IsCargoSale      { get; init; }
    public string? Note            { get; init; }
}
