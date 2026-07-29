using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Plan;

/// <summary>
/// Versioned, date-effective price for a provider plan (BE-P4, §4/§13.1/§13.2). Source of truth for the amount
/// actually charged/snapshotted at subscribe and renewal — <c>ProviderPlanEntity.MonthlyPriceTRY</c> is kept for
/// display/legacy only. Prices for a given (plan, currency, billing period) form a consecutive, non-overlapping,
/// gap-free chain of half-open ranges [EffectiveFrom, EffectiveTo); exactly one is active at any instant.
///
/// <para><see cref="PriceType"/> (Launch/List) is a label for reporting only — resolution is strictly date-driven.
/// Immutable-ish audit entity: private setters, mutated only via Update/Deactivate; built via the validating
/// <see cref="Create"/> factory.</para>
/// </summary>
[DocumentationInfo("Provider plan price entity",
    "Versioned date-effective plan price. Point-in-time resolution over [EffectiveFrom, EffectiveTo); exactly one " +
    "active price per (plan, currency, billing period). Launch = global go-live window; List = afterwards.")]
public sealed class ProviderPlanPriceEntity : AizenEntityWithAudit
{
    public long                  ProviderPlanId { get; private set; }
    public ProviderPlanPriceType PriceType      { get; private set; }   // label/reporting only
    public BillingPeriod         BillingPeriod  { get; private set; }
    public decimal               PriceAmount    { get; private set; }   // the period price (TRY)
    public string                CurrencyCode   { get; private set; } = "TRY";
    public DateTime              EffectiveFrom  { get; private set; }
    public DateTime?             EffectiveTo    { get; private set; }    // null = open-ended
    public CommissionRuleStatus  Status         { get; private set; }
    public string?               PriceCode      { get; private set; }
    public string?               Notes          { get; private set; }

    private ProviderPlanPriceEntity() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ProviderPlanPriceEntity Create(
        long                  providerPlanId,
        ProviderPlanPriceType priceType,
        BillingPeriod         billingPeriod,
        decimal               priceAmount,
        string                currencyCode,
        DateTime              effectiveFrom,
        DateTime?             effectiveTo,
        string?               priceCode,
        string?               notes = null)
    {
        Validate(priceAmount, effectiveFrom, effectiveTo);

        return new ProviderPlanPriceEntity
        {
            ProviderPlanId = providerPlanId,
            PriceType      = priceType,
            BillingPeriod  = billingPeriod,
            PriceAmount    = priceAmount,
            CurrencyCode   = currencyCode.ToUpperInvariant(),
            EffectiveFrom  = effectiveFrom,
            EffectiveTo    = effectiveTo,
            PriceCode      = priceCode,
            Notes          = notes,
            IsActive       = true,
            Status         = DeriveStatus(effectiveFrom, effectiveTo),
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Update(
        ProviderPlanPriceType priceType,
        BillingPeriod         billingPeriod,
        decimal               priceAmount,
        DateTime              effectiveFrom,
        DateTime?             effectiveTo,
        string?               notes)
    {
        Validate(priceAmount, effectiveFrom, effectiveTo);

        PriceType     = priceType;
        BillingPeriod = billingPeriod;
        PriceAmount   = priceAmount;
        EffectiveFrom = effectiveFrom;
        EffectiveTo   = effectiveTo;
        Notes         = notes;
        Status        = DeriveStatus(effectiveFrom, effectiveTo);
    }

    public void Deactivate()
    {
        IsActive = false;
        Status   = CommissionRuleStatus.Inactive;
    }

    public void SetPriceCode(string priceCode) => PriceCode = priceCode;

    /// <summary>Half-open membership: EffectiveFrom ≤ atUtc &lt; EffectiveTo (upper bound belongs to the next record).</summary>
    public bool CoversInstant(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo > atUtc);

    // ── Validation / helpers ────────────────────────────────────────────────────

    private static void Validate(decimal priceAmount, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (priceAmount < 0m)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProviderPlanPriceConflict, "PriceAmount must be ≥ 0.");

        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProviderPlanPriceConflict, "EffectiveTo must be after EffectiveFrom.");
    }

    private static CommissionRuleStatus DeriveStatus(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var now = DateTime.UtcNow;
        if (effectiveFrom > now) return CommissionRuleStatus.Scheduled;
        if (effectiveTo.HasValue && effectiveTo.Value <= now) return CommissionRuleStatus.Expired;
        return CommissionRuleStatus.Active;
    }
}
