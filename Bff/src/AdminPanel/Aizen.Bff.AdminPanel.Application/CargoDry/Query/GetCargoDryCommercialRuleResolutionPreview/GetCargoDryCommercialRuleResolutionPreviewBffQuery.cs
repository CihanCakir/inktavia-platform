using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryCommercialRuleResolutionPreview;

/// <summary>
/// BFF query that proxies to the CargoDry commercial module's generic rule-resolution-preview endpoint.
/// Runs the 7-tier commercial rule resolver in read-only mode against the supplied inputs.
/// Returns the full CargoDryCommercialRuleResolutionBffDto — never throws for business ineligibility.
/// Phase 5 (July 2026).
/// </summary>
public sealed class GetCargoDryCommercialRuleResolutionPreviewBffQuery
    : AizenQuery<GetCargoDryCommercialRuleResolutionPreviewBffQueryResponse>
{
    public string   ProductCode            { get; init; } = default!;
    public int      SalesChannel           { get; init; }
    public int      CommercialModel        { get; init; }
    public string   CurrencyCode           { get; init; } = default!;
    public long?    ProviderProfileId      { get; init; }
    public decimal? SalePrice              { get; init; }
    public long?    ConsignmentAgreementId { get; init; }
    public decimal? AdminOverrideRate      { get; init; }
    public DateTime? EffectiveAtUtc        { get; init; }
}

public sealed class GetCargoDryCommercialRuleResolutionPreviewBffQueryResponse
{
    public CargoDryCommercialRuleResolutionBffDto? Resolution { get; init; }
}
