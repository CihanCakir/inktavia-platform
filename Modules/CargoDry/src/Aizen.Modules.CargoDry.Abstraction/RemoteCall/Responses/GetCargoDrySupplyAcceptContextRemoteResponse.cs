namespace Aizen.Modules.CargoDry.Abstraction.RemoteCall.Responses;

/// <summary>
/// CargoDry → ServiceRequest: server-authoritative context to gate a provider's accept of a CARGODRY_SUPPLY request
/// and pin the offer to the product's fixed retail price.
/// </summary>
public sealed class GetCargoDrySupplyAcceptContextRemoteResponse
{
    public bool    ProductActive              { get; init; }
    public decimal RetailPrice                { get; init; }
    public string? CurrencyCode               { get; init; }
    public bool    ProviderHasActiveAgreement { get; init; }
}
