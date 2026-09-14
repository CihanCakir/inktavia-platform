using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Offers;

/// <summary>
/// CargoDry supply flow — provider "accepts" a CARGODRY_SUPPLY request. No bidding: the SR module pins a single offer
/// at the product's fixed retail price and enforces the program-membership gate server-side. Provider identity is
/// asserted from the token; the BFF passes no price.
/// </summary>
public sealed class CargoDryAcceptBffCommand : AizenCommand<CargoDryAcceptBffResponse>
{
    public long ServiceRequestId { get; init; }
}

public sealed class CargoDryAcceptBffResponse
{
    public bool     Success          { get; init; }
    public string?  Message          { get; init; }
    public long?    OfferId          { get; init; }
    public long?    ServiceRequestId { get; init; }
    public decimal? RetailPrice      { get; init; }
    public string?  CurrencyCode     { get; init; }
}
