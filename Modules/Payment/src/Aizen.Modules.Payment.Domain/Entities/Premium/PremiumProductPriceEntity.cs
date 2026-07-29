using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Premium;

/// <summary>
/// BE-P11 §9.1/§13.9 — a versioned, date-effective price for a premium product (mirrors BE-P4 <c>ProviderPlanPriceEntity</c>).
/// Prices for a given (product, currency) form a consecutive, non-overlapping, gap-free chain of half-open ranges
/// [EffectiveFrom, EffectiveTo); exactly one is active at any instant. The <b>resolved</b> price is snapshotted onto the
/// <see cref="PremiumPurchaseEntity"/> at purchase, so a later price change never affects a past purchase.
/// </summary>
[DocumentationInfo("Premium product price entity",
    "Versioned date-effective premium price. Point-in-time resolution over [EffectiveFrom, EffectiveTo); exactly one active per (product, currency).")]
public sealed class PremiumProductPriceEntity : AizenEntityWithAudit
{
    public long                 PremiumProductId { get; private set; }
    public decimal              PriceAmount      { get; private set; }
    public string               CurrencyCode     { get; private set; } = "TRY";
    public DateTime             EffectiveFrom    { get; private set; }
    public DateTime?            EffectiveTo      { get; private set; }   // null = open-ended
    public CommissionRuleStatus Status           { get; private set; }
    public string?              PriceCode        { get; private set; }
    public string?              Notes            { get; private set; }

    private PremiumProductPriceEntity() { }

    public static PremiumProductPriceEntity Create(
        long premiumProductId, decimal priceAmount, string currencyCode,
        DateTime effectiveFrom, DateTime? effectiveTo, string? priceCode, string? notes = null)
    {
        Validate(priceAmount, effectiveFrom, effectiveTo);

        return new PremiumProductPriceEntity
        {
            PremiumProductId = premiumProductId,
            PriceAmount      = priceAmount,
            CurrencyCode     = currencyCode.ToUpperInvariant(),
            EffectiveFrom    = effectiveFrom,
            EffectiveTo      = effectiveTo,
            PriceCode        = priceCode,
            Notes            = notes,
            IsActive         = true,
            Status           = DeriveStatus(effectiveFrom, effectiveTo),
        };
    }

    public void Update(decimal priceAmount, DateTime effectiveFrom, DateTime? effectiveTo, string? notes)
    {
        Validate(priceAmount, effectiveFrom, effectiveTo);
        PriceAmount   = priceAmount;
        EffectiveFrom = effectiveFrom;
        EffectiveTo   = effectiveTo;
        Notes         = notes;
        Status        = DeriveStatus(effectiveFrom, effectiveTo);
    }

    public void Deactivate() { IsActive = false; Status = CommissionRuleStatus.Inactive; }
    public void SetPriceCode(string priceCode) => PriceCode = priceCode;

    /// <summary>Half-open membership: EffectiveFrom ≤ atUtc &lt; EffectiveTo (upper bound belongs to the next record).</summary>
    public bool CoversInstant(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo > atUtc);

    private static void Validate(decimal priceAmount, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (priceAmount < 0m)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductPriceConflict, "PriceAmount must be ≥ 0.");
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductPriceConflict, "EffectiveTo must be after EffectiveFrom.");
    }

    private static CommissionRuleStatus DeriveStatus(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var now = DateTime.UtcNow;
        if (effectiveFrom > now) return CommissionRuleStatus.Scheduled;
        if (effectiveTo.HasValue && effectiveTo.Value <= now) return CommissionRuleStatus.Expired;
        return CommissionRuleStatus.Active;
    }
}
