namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

/// <summary>
/// CargoDry supply flow — result of a program provider accepting a CARGODRY_SUPPLY request: the server-pinned,
/// non-editable retail offer it created (owner then accepts it to pay). In Abstraction so the provider BFF can consume it.
/// </summary>
public sealed class CreateCargoDrySupplyOfferResponse
{
    public long    OfferId          { get; init; }
    public long    ServiceRequestId { get; init; }
    public decimal RetailPrice      { get; init; }
    public string  CurrencyCode     { get; init; } = default!;
}
