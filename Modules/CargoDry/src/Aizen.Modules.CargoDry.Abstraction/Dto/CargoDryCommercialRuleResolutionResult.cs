namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Output of the CargoDry commercial rule resolver.
/// Describes which rule tier resolved the commission rate, the resulting financial split,
/// and any blocking reasons or warnings produced during resolution.
///
/// Phase 5 (July 2026): CargoDry Commercial Rule Resolver & Commission/Rate Engine.
/// </summary>
public sealed class CargoDryCommercialRuleResolutionResult
{
    /// <summary>True if the resolver found a valid rate. False means <see cref="BlockingReasons"/> is non-empty.</summary>
    public bool CanResolve { get; init; }

    /// <summary>Resolved commission rate (0.00–1.00). Null when <see cref="CanResolve"/> is false.</summary>
    public decimal? ResolvedRate { get; init; }

    /// <summary>Currency code the resolution is denominated in.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>
    /// Source tag identifying which tier produced the rate.
    /// Values: AdminOverride | CommissionRuleProviderSpecific | CommissionRuleProductChannel |
    ///         ConsignmentAgreement | CargoDryProductDefault | DirectSaleNoProviderShare | Unresolved.
    /// </summary>
    public string? RuleSource { get; init; }

    /// <summary>
    /// Id of the <c>CommissionRuleEntity</c> used, if the tier was a commission-rule lookup.
    /// Null for AdminOverride, ConsignmentAgreement, CargoDryProductDefault, and DirectSaleNoProviderShare.
    /// </summary>
    public long? RuleId { get; init; }

    /// <summary>Human-readable name of the matched rule (copied from CommissionRuleEntity.RuleName).</summary>
    public string? RuleName { get; init; }

    /// <summary>Sale price passed in the request (echoed for convenience).</summary>
    public decimal? SalePrice { get; init; }

    /// <summary>
    /// Provider's share of the sale: SalePrice × ResolvedRate.
    /// Null when SalePrice was not provided or when CanResolve is false.
    /// </summary>
    public decimal? ProviderShareAmount { get; init; }

    /// <summary>
    /// Platform's retained share: SalePrice − ProviderShareAmount.
    /// Null when SalePrice was not provided or when CanResolve is false.
    /// </summary>
    public decimal? PlatformShareAmount { get; init; }

    /// <summary>
    /// Reasons the resolver could not produce a rate.
    /// Non-empty only when <see cref="CanResolve"/> is false.
    /// </summary>
    public IReadOnlyList<string> BlockingReasons { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Non-fatal warnings produced during resolution
    /// (e.g., "ConsignmentAgreement found but has no ConsignmentRate — falling through to product default").
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
