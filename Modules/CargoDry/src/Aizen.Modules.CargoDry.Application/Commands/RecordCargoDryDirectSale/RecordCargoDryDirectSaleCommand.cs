using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Commands.RecordCargoDryDirectSale;

/// <summary>
/// CargoDry supply v2 — records the retail revenue of a CARGODRY (direct online / cargo) sale where NO program provider
/// accepted. No provider, no commission, never enters SellThroughSettlement. Idempotent per source SR.
/// </summary>
public sealed class RecordCargoDryDirectSaleCommand : AizenCommand<RecordCargoDryDirectSaleResponse>
{
    public long     ServiceRequestId { get; init; }
    public string   ProductCode      { get; init; } = default!;
    public decimal  SaleAmount       { get; init; }
    public string   CurrencyCode     { get; init; } = default!;
    public long?    KitId            { get; init; }
    public string?  TrackingCode     { get; init; }
    public DateTime? ShippedAtUtc    { get; init; }
}

public sealed class RecordCargoDryDirectSaleResponse
{
    public bool  Recorded     { get; init; }
    public long? DirectSaleId { get; init; }
    public string? Note       { get; init; }
}
