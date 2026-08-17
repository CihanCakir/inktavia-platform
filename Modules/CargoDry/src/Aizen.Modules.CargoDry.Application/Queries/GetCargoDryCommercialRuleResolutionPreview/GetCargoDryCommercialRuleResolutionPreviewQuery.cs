using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryCommercialRuleResolutionPreview;

/// <summary>
/// Preview query — runs the CargoDry commercial rule resolver with the given inputs
/// and returns the full resolution result without persisting anything.
///
/// Useful for admin tooling to verify which rule tier will be applied before
/// calling the resolve-financials command.
///
/// Phase 5 (July 2026): CargoDry Commercial Rule Resolver.
/// </summary>
[DocumentationInfo("Get CargoDry commercial rule resolution preview query",
    "Runs the CargoDry commercial rule resolver in read-only mode and returns the resolution result " +
    "including the matched rule tier, resolved rate, and calculated provider/platform share amounts. " +
    "Does not persist anything. Phase 5 (July 2026).")]
public sealed class GetCargoDryCommercialRuleResolutionPreviewQuery
    : AizenQuery<GetCargoDryCommercialRuleResolutionPreviewResponse>
{
    /// <summary>Provider profile ID. Null for DirectSale.</summary>
    public long? ProviderProfileId { get; init; }

    /// <summary>CargoDry product code. Required.</summary>
    public string ProductCode { get; init; } = default!;

    /// <summary>Sales channel to evaluate rules against.</summary>
    public SalesChannel SalesChannel { get; init; }

    /// <summary>Commercial model to evaluate.</summary>
    public CargoDryCommercialModel CommercialModel { get; init; }

    /// <summary>ISO 4217 currency code (e.g. "TRY", "USD").</summary>
    public string CurrencyCode { get; init; } = default!;

    /// <summary>Sale price for share amount calculations. Optional.</summary>
    public decimal? SalePrice { get; init; }

    /// <summary>Consignment agreement ID (ConsignmentSellThrough only). Optional.</summary>
    public long? ConsignmentAgreementId { get; init; }

    /// <summary>Admin override rate (0.00–1.00). When provided, triggers tier-1 resolution.</summary>
    public decimal? AdminOverrideRate { get; init; }

    /// <summary>UTC time to evaluate rule effectivity against. Defaults to UtcNow when not provided.</summary>
    public DateTime? EffectiveAtUtc { get; init; }
}

public sealed class GetCargoDryCommercialRuleResolutionPreviewResponse
{
    public CargoDryCommercialRuleResolutionResult Resolution { get; init; } = default!;
}
