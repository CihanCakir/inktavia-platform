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
    /// <summary>Derived owner order status (AwaitingShipment | Shipped | ...).</summary>
    public string?   OrderStatus               { get; set; }
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
