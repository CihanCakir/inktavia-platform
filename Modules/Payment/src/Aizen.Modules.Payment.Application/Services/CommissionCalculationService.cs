using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Calculates the commission breakdown for a payment transaction.
///
/// ── Commission precedence chain ──────────────────────────────────────────────
///   1. ProviderOverride   (most specific — per-provider negotiated rate)
///   2. ProviderPlan rate  (plan-level — e.g. PREMIUM_PARTNER = 8%)
///   3. Category rate      (service category — e.g. "ENGINE_MAINTENANCE" = 12%)
///   4. Global default     (COMMISSION_RATE_DEFAULT system param — fallback)
///
/// ── VatRate ──────────────────────────────────────────────────────────────────
///   Read from system parameter COMMISSION_VAT_RATE (decimal, e.g. 0.20 for 20%).
///   Turkey KDV on commission income as of 2023 = 20%.
///   Falls back to 0.20 if parameter is not seeded.
///   Never hardcoded here — change in ReferenceData system parameters to take effect.
///
/// ── Formula ──────────────────────────────────────────────────────────────────
///   commissionBase   = grossAmount - discountAmount
///   commissionAmount = Round(commissionBase × commissionRate, 4)
///   vatOnCommission  = Round(commissionAmount × vatRate, 4)
///   netPayoutAmount  = grossAmount - commissionAmount - vatOnCommission - discountAmount
///
///   Invariants:
///   • commissionAmount must never exceed commissionBase
///   • netPayoutAmount must never be negative
///   • grossAmount must be > 0
/// </summary>
public sealed class CommissionCalculationService
{
    private readonly ICommissionRuleRepository         _rules;
    private readonly ISystemParameterReferenceService  _systemParams;
    private readonly ILogger<CommissionCalculationService> _logger;

    // ── System parameter keys ─────────────────────────────────────────────────
    private const string VatRateParamKey            = "COMMISSION_VAT_RATE";
    private const decimal DefaultVatRate            = 0.20m; // Turkish KDV 20% (as of 2023)
    private const decimal MaxSafeCommissionRate     = 0.50m; // Circuit breaker: no rate > 50%
    private const decimal MinNetPayoutThreshold     = 0.01m; // Net payout must be at least 1 kuruş

    public CommissionCalculationService(
        ICommissionRuleRepository rules,
        ISystemParameterReferenceService systemParams,
        ILogger<CommissionCalculationService> logger)
    {
        _rules        = rules;
        _systemParams = systemParams;
        _logger       = logger;
    }

    /// <summary>
    /// Calculates commission breakdown using the data-driven precedence chain.
    /// All rates are fetched from the database — nothing is hardcoded.
    /// </summary>
    /// <param name="grossAmount">Total amount charged to the payer (TRY). Must be > 0.</param>
    /// <param name="discountAmount">Participant plan discount already applied. Reduces commission base.</param>
    /// <param name="providerProfileId">For ProviderOverride lookup.</param>
    /// <param name="providerPlanId">For Plan-level rate lookup.</param>
    /// <param name="categoryCode">For Category-level rate lookup.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<CommissionBreakdown> CalculateAsync(
        decimal  grossAmount,
        decimal  discountAmount    = 0m,
        long?    providerProfileId = null,
        long?    providerPlanId    = null,
        string?  categoryCode      = null,
        CancellationToken ct = default)
    {
        // ── Guard: grossAmount ────────────────────────────────────────────────
        if (grossAmount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(grossAmount),
                $"grossAmount must be > 0. Received: {grossAmount}");

        if (discountAmount < 0m)
            throw new ArgumentOutOfRangeException(nameof(discountAmount),
                $"discountAmount must be >= 0. Received: {discountAmount}");

        if (discountAmount >= grossAmount)
            throw new InvalidOperationException(
                $"discountAmount ({discountAmount}) cannot equal or exceed grossAmount ({grossAmount}). " +
                $"A 100% discount makes the transaction free — no payment or commission applies.");

        // ── Resolve commission rate via precedence chain ───────────────────────
        var atUtc = DateTime.UtcNow;
        var rate = await _rules.ResolveRateAsync(providerProfileId, providerPlanId, categoryCode, atUtc, ct);

        if (rate is null)
        {
            _logger.LogError(
                "No commission rule found. ProviderProfileId={ProviderId} ProviderPlanId={PlanId} CategoryCode={Category}. " +
                "Ensure a Global commission rule is seeded in the database.",
                providerProfileId, providerPlanId, categoryCode);

            throw new InvalidOperationException(
                "No commission rule found. At minimum, a Global commission rule must be seeded " +
                "(CommissionRuleType.Global with EffectiveTo = null). " +
                $"Context: ProviderProfileId={providerProfileId}, ProviderPlanId={providerPlanId}, CategoryCode={categoryCode}");
        }

        // ── Circuit breaker: rate sanity ──────────────────────────────────────
        if (rate > MaxSafeCommissionRate)
        {
            _logger.LogCritical(
                "Commission rate {Rate:P2} exceeds safety threshold {Max:P2}. " +
                "This is likely a data entry error. Blocking calculation.",
                rate, MaxSafeCommissionRate);

            throw new InvalidOperationException(
                $"Commission rate {rate:P2} exceeds the safety threshold of {MaxSafeCommissionRate:P2}. " +
                $"Check the CommissionRule data.");
        }

        if (rate < 0m)
            throw new InvalidOperationException(
                $"Commission rate must not be negative. Rate resolved: {rate}");

        // ── Read VatRate from system parameter ─────────────────────────────────
        var vatRate = await _systemParams.GetDecimalAsync(VatRateParamKey, ct)
                      ?? DefaultVatRate;

        if (vatRate < 0m || vatRate > 1m)
        {
            _logger.LogWarning(
                "System param {Key} has invalid value {Value}. Falling back to default {Default}.",
                VatRateParamKey, vatRate, DefaultVatRate);
            vatRate = DefaultVatRate;
        }

        // ── Commission calculation ─────────────────────────────────────────────
        //
        //   commissionBase   = grossAmount - discountAmount
        //   commissionAmount = ROUND(commissionBase × rate, 4 decimals, AwayFromZero)
        //   vatOnCommission  = ROUND(commissionAmount × vatRate, 4 decimals, AwayFromZero)
        //   netPayoutAmount  = grossAmount - commissionAmount - vatOnCommission - discountAmount
        //
        var commissionBase   = grossAmount - discountAmount;
        var commissionAmount = Math.Round(commissionBase * rate,         4, MidpointRounding.AwayFromZero);
        var vatOnCommission  = Math.Round(commissionAmount * vatRate,    4, MidpointRounding.AwayFromZero);
        var netPayoutAmount  = grossAmount - commissionAmount - vatOnCommission - discountAmount;

        // ── Invariant: net payout must not be negative ─────────────────────────
        if (netPayoutAmount < MinNetPayoutThreshold)
        {
            _logger.LogError(
                "Commission calculation produced a net payout below threshold. " +
                "GrossAmount={Gross} DiscountAmount={Discount} CommissionRate={Rate:P2} " +
                "CommissionAmount={Commission} VatRate={VatRate:P2} VatOnCommission={Vat} " +
                "NetPayout={Net}",
                grossAmount, discountAmount, rate, commissionAmount, vatRate, vatOnCommission, netPayoutAmount);

            throw new InvalidOperationException(
                $"Commission calculation produced a non-positive net payout ({netPayoutAmount:F4} {nameof(netPayoutAmount)}). " +
                $"Gross={grossAmount}, Discount={discountAmount}, Rate={rate:P2}, Commission={commissionAmount}, " +
                $"VAT={vatOnCommission}, Net={netPayoutAmount}. " +
                $"Review commission rules and discount amounts.");
        }

        // ── Resolve which precedence tier was matched ──────────────────────────
        //    (used for audit / logging only — does not affect calculation)
        var appliedTier = ResolveAppliedTier(providerProfileId, providerPlanId, categoryCode, rate);

        _logger.LogInformation(
            "Commission calculated. " +
            "GrossAmount={Gross} DiscountAmount={Discount} CommissionBase={Base} " +
            "AppliedTier={Tier} CommissionRate={Rate:P2} CommissionAmount={Commission} " +
            "VatRate={VatRate:P2} VatOnCommission={Vat} NetPayoutAmount={Net}",
            grossAmount, discountAmount, commissionBase,
            appliedTier, rate, commissionAmount,
            vatRate, vatOnCommission, netPayoutAmount);

        return new CommissionBreakdown(
            GrossAmount:      grossAmount,
            DiscountAmount:   discountAmount,
            CommissionBase:   commissionBase,
            CommissionRate:   rate,
            CommissionAmount: commissionAmount,
            VatRate:          vatRate,
            VatOnCommission:  vatOnCommission,
            NetPayoutAmount:  netPayoutAmount,
            AppliedTier:      appliedTier
        );
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Heuristically identifies which precedence tier produced this rate.
    /// Used for audit logging only.
    /// </summary>
    private static CommissionRateTier ResolveAppliedTier(
        long? providerProfileId, long? providerPlanId, string? categoryCode, decimal rate)
    {
        // The actual lookup is done by ICommissionRuleRepository.ResolveRateAsync.
        // Here we just annotate: if context was provided, the tier is that context at minimum.
        // Exact tier cannot be determined without a second DB call — log context instead.
        if (providerProfileId.HasValue) return CommissionRateTier.ProviderOverrideOrFallback;
        if (providerPlanId.HasValue)    return CommissionRateTier.PlanOrFallback;
        if (categoryCode is not null)   return CommissionRateTier.CategoryOrFallback;
        return CommissionRateTier.GlobalDefault;
    }
}

// ── Result ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Full money breakdown for a payment transaction.
/// All amounts are in the transaction's currency (default TRY).
/// </summary>
public sealed record CommissionBreakdown(
    /// <summary>Total charged to the payer.</summary>
    decimal GrossAmount,

    /// <summary>Participant plan discount absorbed by the platform.</summary>
    decimal DiscountAmount,

    /// <summary>GrossAmount - DiscountAmount. Commission is calculated on this base.</summary>
    decimal CommissionBase,

    /// <summary>The effective commission rate (snapshot at time of calculation).</summary>
    decimal CommissionRate,

    /// <summary>Commission charged to the provider: CommissionBase × CommissionRate.</summary>
    decimal CommissionAmount,

    /// <summary>VAT rate applied to commission income (KDV). Sourced from COMMISSION_VAT_RATE param.</summary>
    decimal VatRate,

    /// <summary>KDV on commission amount: CommissionAmount × VatRate.</summary>
    decimal VatOnCommission,

    /// <summary>
    /// Net amount transferred to the provider after all deductions:
    /// GrossAmount - CommissionAmount - VatOnCommission - DiscountAmount.
    /// </summary>
    decimal NetPayoutAmount,

    /// <summary>Which precedence tier produced the commission rate (for audit).</summary>
    CommissionRateTier AppliedTier
);

/// <summary>
/// Indicates which tier of the commission precedence chain produced the effective rate.
/// ProviderOverride > Plan > Category > GlobalDefault
/// </summary>
public enum CommissionRateTier
{
    GlobalDefault                = 0,
    CategoryOrFallback           = 1,
    PlanOrFallback               = 2,
    ProviderOverrideOrFallback   = 3,
}
