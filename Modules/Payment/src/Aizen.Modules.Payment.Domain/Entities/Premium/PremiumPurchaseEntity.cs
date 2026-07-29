using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Premium;

/// <summary>
/// BE-P11 §9.2/§13.9 — a provider's purchase of a premium product for a specific offer. <b>Snapshots the RESOLVED price,
/// duration, and currency at purchase</b> so a later price change never affects a past purchase. Payment-driven lifecycle:
/// Pending (checkout initiated) → Paid (success webhook → entitlement created) → Refunded (boost refund → entitlement
/// revoked); or Pending → Failed. <see cref="ContextRef"/> is the offer id the boost targets.
/// </summary>
[DocumentationInfo("Premium purchase entity",
    "A provider's premium purchase for an offer. Snapshots the resolved price/duration; Pending→Paid→Refunded / Pending→Failed.")]
public sealed class PremiumPurchaseEntity : AizenEntityWithAudit
{
    public long                  ProviderProfileId             { get; private set; }
    public long                  PremiumProductId              { get; private set; }
    public string                ProductCodeSnapshot           { get; private set; } = default!;
    public long                  PremiumProductPriceIdSnapshot { get; private set; }
    public decimal               UnitPriceSnapshot             { get; private set; }
    public string                CurrencyCodeSnapshot          { get; private set; } = "TRY";
    public int                   DurationDaysSnapshot          { get; private set; }
    /// <summary>The offer id the boost targets (§9.2). Carried onto the entitlement.</summary>
    public long                  ContextRef                    { get; private set; }
    public long?                 PaymentTransactionId          { get; private set; }
    public PremiumPurchaseStatus Status                        { get; private set; }
    public string                PurchaseCode                  { get; private set; } = default!;

    private PremiumPurchaseEntity() { }

    public static PremiumPurchaseEntity Create(
        long providerProfileId, long premiumProductId, string productCodeSnapshot, long premiumProductPriceIdSnapshot,
        decimal unitPriceSnapshot, string currencyCodeSnapshot, int durationDaysSnapshot,
        long contextRef, string purchaseCode)
    {
        if (unitPriceSnapshot < 0m)    throw new ArgumentException("UnitPrice must be ≥ 0.", nameof(unitPriceSnapshot));
        if (durationDaysSnapshot <= 0) throw new ArgumentException("Duration must be positive.", nameof(durationDaysSnapshot));

        return new PremiumPurchaseEntity
        {
            ProviderProfileId             = providerProfileId,
            PremiumProductId              = premiumProductId,
            ProductCodeSnapshot           = productCodeSnapshot,
            PremiumProductPriceIdSnapshot = premiumProductPriceIdSnapshot,
            UnitPriceSnapshot             = unitPriceSnapshot,
            CurrencyCodeSnapshot          = currencyCodeSnapshot.ToUpperInvariant(),
            DurationDaysSnapshot          = durationDaysSnapshot,
            ContextRef                    = contextRef,
            Status                        = PremiumPurchaseStatus.Pending,
            PurchaseCode                  = purchaseCode,
            IsActive                      = true,
        };
    }

    /// <summary>Links the payment transaction created for the checkout (Pending stays Pending until the webhook).</summary>
    public void LinkTransaction(long paymentTransactionId) => PaymentTransactionId = paymentTransactionId;

    /// <summary>§9.2 — success webhook: Pending → Paid. Idempotent (already Paid → no-op).</summary>
    public void MarkPaid()
    {
        if (Status == PremiumPurchaseStatus.Paid) return;
        if (Status != PremiumPurchaseStatus.Pending)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumPurchaseInvalidState,
                $"Cannot mark Paid from {Status}.");
        Status = PremiumPurchaseStatus.Paid;
    }

    /// <summary>§9.2 — failure webhook: Pending → Failed. Idempotent.</summary>
    public void MarkFailed()
    {
        if (Status == PremiumPurchaseStatus.Failed) return;
        if (Status != PremiumPurchaseStatus.Pending)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumPurchaseInvalidState,
                $"Cannot mark Failed from {Status}.");
        Status = PremiumPurchaseStatus.Failed;
    }

    /// <summary>§9.2 — boost refund: Paid → Refunded. Idempotent (already Refunded → no-op).</summary>
    public void MarkRefunded()
    {
        if (Status == PremiumPurchaseStatus.Refunded) return;
        if (Status != PremiumPurchaseStatus.Paid)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumPurchaseInvalidState,
                $"Cannot mark Refunded from {Status}.");
        Status = PremiumPurchaseStatus.Refunded;
    }
}
