namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

/// <summary>
/// CargoDry supply v2 (F1) — admin listing row for cargo (direct online sale) supply orders: those in AwaitingShipment
/// or Shipped. Read-only projection for the admin fulfilment queue.
/// </summary>
public sealed class CargoDrySupplyOrderAdminDto
{
    public long      ServiceRequestId          { get; set; }
    public string    RequestCode               { get; set; } = default!;
    public string?   ProductCode               { get; set; }
    public string?   VesselName                { get; set; }
    /// <summary>Owner user id — carried so the admin BFF can enrich <see cref="OwnerName"/> from Identity.</summary>
    public long      OwnerUserId               { get; set; }
    /// <summary>Owner display name. Resolved by the admin BFF (Identity lookup); null when unavailable.</summary>
    public string?   OwnerName                 { get; set; }
    /// <summary>Derived owner order status (AwaitingShipment | Shipped | Completed | Cancelled | ...).</summary>
    public string?   OrderStatus               { get; set; }
    /// <summary>Fulfilment path: "ProviderFulfilled" (a program provider was assigned) or "CargoDirectSale"
    /// (no provider accepted → cargo/direct-online sale). Derived from the assignment presence.</summary>
    public string?   OrderPath                 { get; set; }
    /// <summary>Assigned provider profile id (null for cargo direct sales).</summary>
    public long?     ProviderProfileId         { get; set; }
    /// <summary>Assigned provider display name (denormalized on the SR, or BFF-enriched); null for cargo sales.</summary>
    public string?   ProviderName              { get; set; }
    /// <summary>When the order completed (from status history → Completed/Closed); null while in-flight.</summary>
    public DateTime? CompletedAtUtc            { get; set; }
    /// <summary>When the order entered AwaitingShipment (from status history; falls back to CreatedAt).</summary>
    public DateTime? AwaitingSince             { get; set; }
    public DateTime? ProviderAcceptDeadlineUtc { get; set; }
    public DateTime? ShippedAtUtc              { get; set; }
    public string?   TrackingCode              { get; set; }
    public decimal?  RetailAmount              { get; set; }
    public string?   CurrencyCode              { get; set; }
    public DateTime  CreatedAt                 { get; set; }
}

public sealed class CargoDrySupplyOrderAdminListDto
{
    public List<CargoDrySupplyOrderAdminDto> Items { get; set; } = new();
    public int Total    { get; set; }
    public int Page     { get; set; }
    public int PageSize { get; set; }
}
