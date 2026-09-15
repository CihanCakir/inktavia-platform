using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.MarkCargoDrySupplyDelivered;

/// <summary>
/// CargoDry supply v2 — the assigned provider marks a supply order delivered, capturing the delivered kit. Starts the
/// delivered auto-complete window (DeliveredAutoCompleteHours): if the owner does not QR-activate within it, the order
/// auto-completes and the sale/commission accrues for the delivered kit. Provider identity is taken from the token.
/// </summary>
public sealed class MarkCargoDrySupplyDeliveredCommand : AizenCommand<MarkCargoDrySupplyDeliveredResponse>
{
    public long ServiceRequestId { get; init; }
    /// <summary>The kit the provider delivered/installed (from their consignment stock).</summary>
    public long KitId            { get; init; }
}

public sealed class MarkCargoDrySupplyDeliveredResponse
{
    public long      ServiceRequestId        { get; init; }
    public DateTime  DeliveredAtUtc          { get; init; }
    public DateTime  AutoCompleteDeadlineUtc { get; init; }
}
