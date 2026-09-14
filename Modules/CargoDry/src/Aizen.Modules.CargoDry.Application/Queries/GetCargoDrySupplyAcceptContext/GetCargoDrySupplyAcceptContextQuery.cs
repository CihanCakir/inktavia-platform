using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySupplyAcceptContext;

/// <summary>
/// CargoDry supply flow — the server-authoritative context the ServiceRequest module needs to (a) GATE a provider's
/// accept of a CARGODRY_SUPPLY request (only active-agreement / program providers may accept) and (b) PIN the offer to
/// the product's fixed retail price. Pure read.
/// </summary>
public sealed class GetCargoDrySupplyAcceptContextQuery : AizenQuery<GetCargoDrySupplyAcceptContextResponse>
{
    public long   ProviderProfileId { get; init; }
    public string ProductCode       { get; init; } = default!;
}

public sealed class GetCargoDrySupplyAcceptContextResponse
{
    /// <summary>False when the product code is unknown or inactive (accept must be blocked).</summary>
    public bool    ProductActive             { get; init; }
    /// <summary>Fixed retail price the offer is pinned to (0 when product not found).</summary>
    public decimal RetailPrice               { get; init; }
    public string? CurrencyCode              { get; init; }
    /// <summary>True when the provider has an ACTIVE ConsignmentAgreement for this product — the program-membership signal.</summary>
    public bool    ProviderHasActiveAgreement { get; init; }
}
