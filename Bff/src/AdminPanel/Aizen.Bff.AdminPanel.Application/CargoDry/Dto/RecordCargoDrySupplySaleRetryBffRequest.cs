namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

/// <summary>
/// Dev/ops reconciliation request body for re-firing the CargoDry supply record-sale. The SR id comes from the route;
/// the owner + acting-admin ids are resolved server-side, so only the sale facts are supplied here.
/// </summary>
public sealed class RecordCargoDrySupplySaleRetryBffRequest
{
    public long    KitId        { get; init; }
    public decimal SaleAmount   { get; init; }
    public string  CurrencyCode { get; init; } = default!;
}
