namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

/// <summary>CargoDry supply v2 — provider marks a supply order delivered, capturing the delivered kit.</summary>
public sealed class MarkCargoDrySupplyDeliveredRequest
{
    public long KitId { get; set; }
}

/// <summary>CargoDry supply v2 — admin marks a cargo order shipped with a tracking code (optionally the shipped kit).</summary>
public sealed class MarkCargoDrySupplyShippedRequest
{
    public string TrackingCode { get; set; } = default!;
    public long?  KitId        { get; set; }
}
