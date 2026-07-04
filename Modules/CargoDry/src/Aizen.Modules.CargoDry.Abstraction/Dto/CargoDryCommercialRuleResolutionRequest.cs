using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Input to the CargoDry commercial rule resolver.
/// The resolver evaluates this context against all rule tiers and returns a
/// <see cref="CargoDryCommercialRuleResolutionResult"/> describing which tier matched
/// and what rate was produced.
///
/// Phase 5 (July 2026): CargoDry Commercial Rule Resolver & Commission/Rate Engine.
/// </summary>
public sealed class CargoDryCommercialRuleResolutionRequest
{
    /// <summary>
    /// Provider profile ID. Required for ProviderResale / ConsignmentSellThrough / ProviderAttributedSale.
    /// Null for DirectSale.
    /// </summary>
    public long? ProviderProfileId { get; init; }

    /// <summary>CargoDry product code (e.g. "CD-MARINE-PRO"). Required.</summary>
    public string ProductCode { get; init; } = default!;

    /// <summary>Sales channel that governs revenue attribution.</summary>
    public SalesChannel SalesChannel { get; init; }

    /// <summary>Commercial model in effect for the kit.</summary>
    public CargoDryCommercialModel CommercialModel { get; init; }

    /// <summary>ISO 4217 currency code the sale is denominated in (e.g. "TRY", "USD").</summary>
    public string CurrencyCode { get; init; } = default!;

    /// <summary>UTC point in time for which the rule must be effective.</summary>
    public DateTime EffectiveAtUtc { get; init; }

    /// <summary>Actual sale price, used to compute provider and platform share amounts.</summary>
    public decimal? SalePrice { get; init; }

    /// <summary>
    /// Consignment agreement ID. Provided for ConsignmentSellThrough channel so the resolver
    /// can read the agreement's <c>ConsignmentRate</c> as a tier-4 fallback.
    /// </summary>
    public long? ConsignmentAgreementId { get; init; }

    /// <summary>
    /// Admin-supplied override rate (0.00–1.00). When present, this is tier-1 — highest priority.
    /// </summary>
    public decimal? AdminOverrideRate { get; init; }

    /// <summary>ID of the user (admin) who triggered the resolution. Recorded in the rule trace.</summary>
    public long? RequestedByUserId { get; init; }
}
