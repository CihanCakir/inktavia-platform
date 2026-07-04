using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySalesAttributionRuleResolutionPreview;

/// <summary>
/// BFF query that proxies to the CargoDry commercial module's attribution-specific
/// rule-resolution-preview endpoint. Loads the sales attribution record from the module
/// and runs the resolver against its context plus any optional override inputs.
/// Returns both the current attribution DTO and the resolution result.
/// Safe to call at any time — never throws for business ineligibility.
/// Phase 5 (July 2026).
/// </summary>
public sealed class GetCargoDrySalesAttributionRuleResolutionPreviewBffQuery
    : AizenQuery<GetCargoDrySalesAttributionRuleResolutionPreviewBffQueryResponse>
{
    /// <summary>Id of the CargoDrySalesAttributionEntity to preview resolution for.</summary>
    public long     SalesAttributionId { get; init; }

    /// <summary>Optional sale price override. Falls back to the attribution's existing SalePrice.</summary>
    public decimal? SalePrice          { get; init; }

    /// <summary>Optional ISO 4217 currency code override. Falls back to the attribution's existing CurrencyCode.</summary>
    public string?  CurrencyCode       { get; init; }

    /// <summary>Optional admin override rate (0.00–1.00). Triggers tier-1 resolution when provided.</summary>
    public decimal? AdminOverrideRate  { get; init; }
}

public sealed class GetCargoDrySalesAttributionRuleResolutionPreviewBffQueryResponse
{
    public CargoDrySalesAttributionRuleResolutionPreviewBffDto? Preview { get; init; }
}
