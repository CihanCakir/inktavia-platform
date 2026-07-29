using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// BE-P11 §9.2/§13.9 — the premium boost lifecycle orchestrator, decoupled from the commission path (§19.4). Called by the
/// webhook handler on a paid <c>PremiumBoostPurchase</c> capture (→ purchase Paid + entitlement created &amp; Active) and by
/// the refund handler on a boost refund (→ purchase Refunded + entitlement Revoked). Also drives the expiration sweep.
/// <para><b>Idempotent everywhere:</b> the unique <c>PremiumPurchaseId</c> on the entitlement + the transaction-level
/// <c>CapturedAt</c> guard mean a duplicate webhook never creates a second entitlement.</para>
/// </summary>
public sealed class PremiumBoostService
{
    private readonly IPremiumPurchaseRepository    _purchases;
    private readonly IPremiumEntitlementRepository _entitlements;
    private readonly FinancialLedgerPostingService _ledgerPosting;
    private readonly ILogger<PremiumBoostService>  _logger;

    public PremiumBoostService(
        IPremiumPurchaseRepository    purchases,
        IPremiumEntitlementRepository entitlements,
        FinancialLedgerPostingService ledgerPosting,
        ILogger<PremiumBoostService>  logger)
    {
        _purchases     = purchases;
        _entitlements  = entitlements;
        _ledgerPosting = ledgerPosting;
        _logger        = logger;
    }

    /// <summary>
    /// §9.2 — success webhook: mark the purchase Paid and create + Activate the single entitlement (now → now +
    /// DurationDaysSnapshot). Idempotent: a duplicate webhook finds the existing entitlement and no-ops. Returns the
    /// entitlement, or null when the transaction is not a tracked premium purchase.
    /// </summary>
    public async Task<PremiumEntitlementEntity?> OnBoostPaidAsync(PaymentTransactionEntity tx, CancellationToken ct)
    {
        var purchase = await _purchases.GetByPaymentTransactionIdAsync(tx.Id, ct);
        if (purchase is null)
        {
            _logger.LogWarning("Premium boost paid webhook: no purchase for Tx {TxId}.", tx.Id);
            return null;
        }

        // Idempotency: a duplicate webhook must not create a second entitlement (unique PremiumPurchaseId).
        var existing = await _entitlements.GetByPurchaseIdAsync(purchase.Id, ct);
        if (existing is not null)
        {
            _logger.LogInformation("Premium boost already activated for purchase {Code}. Idempotent webhook.", purchase.PurchaseCode);
            return existing;
        }

        purchase.MarkPaid();
        _purchases.Update(purchase);

        var now = DateTime.UtcNow;
        var entitlement = PremiumEntitlementEntity.CreateInactive(
            purchase.Id, purchase.ProviderProfileId, purchase.ProductCodeSnapshot, purchase.ContextRef);
        entitlement.Activate(now, now.AddDays(purchase.DurationDaysSnapshot));
        await _entitlements.AddAsync(entitlement, ct);

        // ── BE-P12: premium revenue = purchase.UnitPriceSnapshot (derived from the immutable purchase). ──
        await _ledgerPosting.PostPremiumPaidAsync(purchase, tx.Id, ct);

        _logger.LogInformation(
            "Premium boost activated. Purchase={Code} Offer={Offer} Provider={Provider} Window={Start:o}→{End:o}",
            purchase.PurchaseCode, purchase.ContextRef, purchase.ProviderProfileId, now, now.AddDays(purchase.DurationDaysSnapshot));
        return entitlement;
    }

    /// <summary>
    /// §9.2 — boost refund: mark the purchase Refunded and Revoke the entitlement. Idempotent (already Refunded/Revoked →
    /// no-op). A boost refund is <b>non-marketplace</b> — no ProviderNegativeBalance (the money was Inktavia's premium revenue).
    /// </summary>
    public async Task OnBoostRefundedAsync(PaymentTransactionEntity tx, string reason, CancellationToken ct)
    {
        var purchase = await _purchases.GetByPaymentTransactionIdAsync(tx.Id, ct);
        if (purchase is null)
        {
            _logger.LogWarning("Premium boost refund: no purchase for Tx {TxId}.", tx.Id);
            return;
        }

        if (purchase.Status == PremiumPurchaseStatus.Paid)
        {
            purchase.MarkRefunded();
            _purchases.Update(purchase);
            // ── BE-P12: reverse the premium revenue (keyed by the purchase — idempotent with a later backfill). ──
            await _ledgerPosting.PostPremiumRefundAsync(purchase, tx.Id, ct);
        }

        var entitlement = await _entitlements.GetByPurchaseIdAsync(purchase.Id, ct);
        if (entitlement is not null && entitlement.Status == PremiumEntitlementStatus.Active)
        {
            entitlement.Revoke(reason);
            _entitlements.Update(entitlement);
            _logger.LogInformation("Premium boost revoked (refund). Purchase={Code} Offer={Offer}.", purchase.PurchaseCode, purchase.ContextRef);
        }
    }

    /// <summary>§13.9 — expire Active entitlements past their ExpiresAt. Returns the count expired.</summary>
    public async Task<int> ExpireDueAsync(DateTime nowUtc, int max, CancellationToken ct)
    {
        var due = await _entitlements.GetExpirableAsync(nowUtc, max, ct);
        foreach (var e in due)
        {
            e.Expire();
            _entitlements.Update(e);
        }
        if (due.Count > 0)
        {
            await _entitlements.SaveChangesAsync(ct);
            _logger.LogInformation("Expired {Count} premium entitlement(s) past ExpiresAt.", due.Count);
        }
        return due.Count;
    }
}
