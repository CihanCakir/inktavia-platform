using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.ResolveCargoDrySalesAttributionFinancials;

/// <summary>
/// Resolves financial amounts (SalePrice, CommissionRate, ProviderShareAmount, PlatformShareAmount)
/// for a single CargoDry sales attribution record.
///
/// Commission rate resolution priority:
///   1. CommissionRate override in this command (if provided)
///   2. ConsignmentRate from the linked ConsignmentAgreement
///   3. ProviderCommissionRate from the CargoDryProduct catalog
///   4. 0.00 (DirectSale default — platform keeps the full SalePrice)
///
/// After resolution, if the attribution belongs to a SellThroughSettlement,
/// the settlement totals are recalculated from all resolved attributions.
///
/// Phase 4A (July 2026): CargoDry Financial Resolution.
/// </summary>
[DocumentationInfo("Resolve CargoDry sales attribution financials command",
    "Sets SalePrice, CommissionRate, ProviderShareAmount, and PlatformShareAmount on a " +
    "CargoDry sales attribution record and recalculates the parent settlement totals if applicable. " +
    "Phase 4A (July 2026).")]
public sealed class ResolveCargoDrySalesAttributionFinancialsCommand : AizenCommand<ResolveCargoDrySalesAttributionFinancialsResponse>
{
    /// <summary>Id of the CargoDrySalesAttributionEntity to resolve.</summary>
    public long    SalesAttributionId { get; init; }

    /// <summary>Actual price the end user paid for the kit (must be > 0).</summary>
    public decimal SalePrice          { get; init; }

    /// <summary>ISO 4217 currency code for SalePrice (e.g. "TRY", "EUR").</summary>
    public string  CurrencyCode       { get; init; } = default!;

    /// <summary>
    /// Optional manual override of the commission rate (0.00–1.00).
    /// When provided, bypasses the automatic rate lookup cascade.
    /// </summary>
    public decimal? CommissionRateOverride { get; init; }

    /// <summary>Admin user performing the resolution. Required for audit trail.</summary>
    public long    ResolvedByUserId   { get; init; }

    /// <summary>Optional note explaining any override or clarification.</summary>
    public string? ResolutionNote     { get; init; }
}

public sealed class ResolveCargoDrySalesAttributionFinancialsResponse
{
    public CargoDrySalesAttributionDto Attribution { get; init; } = default!;
}
