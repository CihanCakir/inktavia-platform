using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionRuleResolutionPreview;

/// <summary>
/// Preview query — loads an existing CargoDrySalesAttribution record and runs the
/// commercial rule resolver against it without persisting anything.
///
/// Returns both the full attribution DTO (current state) and the resolved rule result
/// so admin tooling can preview what rate would apply before calling resolve-financials.
///
/// Phase 5 (July 2026): CargoDry Commercial Rule Resolver.
/// </summary>
[DocumentationInfo("Get CargoDry sales attribution rule resolution preview query",
    "Loads an existing sales attribution record and runs the commercial rule resolver against it " +
    "in read-only mode. Returns the current attribution state plus the resolution result. " +
    "Does not persist anything. Phase 5 (July 2026).")]
public sealed class GetCargoDrySalesAttributionRuleResolutionPreviewQuery
    : AizenQuery<GetCargoDrySalesAttributionRuleResolutionPreviewResponse>
{
    /// <summary>Id of the CargoDrySalesAttributionEntity to preview resolution for.</summary>
    public long SalesAttributionId { get; init; }

    /// <summary>
    /// Sale price to use for share amount calculations.
    /// If not provided, any existing SalePrice on the attribution is used.
    /// </summary>
    public decimal? SalePrice { get; init; }

    /// <summary>
    /// ISO 4217 currency code. If not provided, the attribution's existing CurrencyCode is used.
    /// </summary>
    public string? CurrencyCode { get; init; }

    /// <summary>Optional admin override rate (0.00–1.00). Triggers tier-1 resolution when provided.</summary>
    public decimal? AdminOverrideRate { get; init; }
}

public sealed class GetCargoDrySalesAttributionRuleResolutionPreviewResponse
{
    public CargoDrySalesAttributionDto           Attribution { get; init; } = default!;
    public CargoDryCommercialRuleResolutionResult Resolution  { get; init; } = default!;
}
