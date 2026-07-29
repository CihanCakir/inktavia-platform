using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Entities.Reporting;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Domain.Money;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// BE-P12 §15/§19.17 — posts the append-only reporting ledger <b>from the immutable sources</b> (never recomputes rates).
/// Every line's amount is read straight off the PaymentEconomicsSnapshot / RefundAllocation / ChargebackRecord /
/// PremiumPurchase / subscription so the ledger reconciles to its source to the cent. Reporting is additive — no
/// economics/refund/premium logic is changed.
/// <para><b>Idempotent</b>: each <c>(SourceType, SourceRef, AccountLine, IsReversal)</c> is posted at most once (pre-check +
/// the unique index backstop), so a duplicate webhook or a backfill re-run never double-posts. A reversal/correction is a
/// NEW entry, never a mutation.</para>
/// <para><b>§19.17 rules enforced:</b> discounts are never merged; provider-funded customer discount is a MEMO (not an
/// Inktavia expense); platform-funded discount IS a campaign expense; platform-fee VAT is a Liability, not revenue.</para>
/// </summary>
public sealed class FinancialLedgerPostingService
{
    private readonly IFinancialLedgerRepository _ledger;
    private readonly ILogger<FinancialLedgerPostingService> _logger;

    /// <summary>Keys posted by THIS instance but not yet committed — catches intra-unit-of-work duplicates that the DB
    /// <c>ExistsAsync</c> pre-check cannot see (e.g. a backfill that posts many sources before a single SaveChanges).</summary>
    private readonly HashSet<string> _pending = new();

    public FinancialLedgerPostingService(IFinancialLedgerRepository ledger, ILogger<FinancialLedgerPostingService> logger)
    {
        _ledger = ledger;
        _logger = logger;
    }

    // ── Acceptance (economics snapshot) ─────────────────────────────────────────

    /// <summary>§15 — posts the revenue/liability/discount/contribution lines for an accepted transaction, reading amounts
    /// straight off the immutable snapshot. Provider/customer ids come from the transaction (the snapshot is offer-scoped).</summary>
    public async Task PostAcceptanceAsync(PaymentTransactionEntity tx, PaymentEconomicsSnapshotEntity s, CancellationToken ct)
    {
        var cur      = s.CurrencyCodeSnapshot;
        var occurred = s.CreatedAtUtc == default ? DateTime.UtcNow : s.CreatedAtUtc;
        long provider = tx.RecipientProfileId ?? 0;
        long customer = tx.PayerProfileId;

        // Revenue (§15) — commission + platform-fee NET (VAT is a separate liability).
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.ProviderCommissionRevenue,
            s.CommissionAmountSnapshot, false, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.CustomerPlatformFeeNetRevenue,
            s.PlatformFeeNetAmountSnapshot, false, cur, tx.Id, provider, customer, occurred, ct);

        // §19.17 — platform-fee VAT is a LIABILITY, not revenue.
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.CustomerPlatformFeeVatLiability,
            s.PlatformFeeVatAmountSnapshot, false, cur, tx.Id, provider, customer, occurred, ct);

        // §19.17 — discounts kept SEPARATE: platform-funded IS a campaign expense; provider-funded is a MEMO (not expense).
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.PlatformFundedCustomerDiscountExpense,
            s.TotalPlatformFundedDiscountSnapshot, false, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.ProviderFundedCustomerDiscount,
            s.TotalProviderFundedDiscountSnapshot, false, cur, tx.Id, provider, customer, occurred, ct);

        // Contribution / settlement memos (§13.10) — derived from the snapshot's own figures.
        var customerSide = s.PlatformFeeNetAmountSnapshot;                          // platform's customer-side take (excl VAT)
        var providerSide = s.CommissionAmountSnapshot;                              // platform's provider-side take
        var totalContribution = MoneyMath.Round(customerSide + providerSide - s.TotalPlatformFundedDiscountSnapshot);
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.PlatformGrossShare,
            s.PlatformGrossShareSnapshot, false, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.CustomerSideContribution,
            customerSide, false, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.ProviderSideContribution,
            providerSide, false, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.TotalTransactionContribution,
            totalContribution, false, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.AcceptanceSnapshot, s.Id, LedgerAccountLine.NetMarketplaceContribution,
            totalContribution, false, cur, tx.Id, provider, customer, occurred, ct);

        _logger.LogInformation("Ledger posted (acceptance) for snapshot {SnapId} tx {TxId}.", s.Id, tx.Id);
    }

    // ── Refund (P10 allocation) ─────────────────────────────────────────────────

    /// <summary>§15 — reverses the affected revenue lines (contra entries) + posts the refund/gateway/recovery/advance
    /// lines from the immutable 9-amount allocation. SourceRef = the refund record id (distinct from the acceptance snapshot).</summary>
    public async Task PostRefundAsync(PaymentTransactionEntity tx, long refundRecordId, RefundAllocationEntity a, CancellationToken ct)
    {
        var cur      = a.CurrencyCode;
        var occurred = DateTime.UtcNow;
        long provider = tx.RecipientProfileId ?? 0;
        long customer = tx.PayerProfileId;

        // Revenue reversals (contra — reduce the recognised revenue).
        await PostAsync(LedgerSourceType.Refund, refundRecordId, LedgerAccountLine.ProviderCommissionRevenue,
            a.CommissionRevenueReversalAmount, true, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.Refund, refundRecordId, LedgerAccountLine.CustomerPlatformFeeNetRevenue,
            a.PlatformFeeNetRefundAmount, true, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.Refund, refundRecordId, LedgerAccountLine.CustomerPlatformFeeVatLiability,
            a.PlatformFeeVatRefundAmount, true, cur, tx.Id, provider, customer, occurred, ct);

        // Expense + recovery/advance from the allocation.
        await PostAsync(LedgerSourceType.Refund, refundRecordId, LedgerAccountLine.RefundProcessingExpense,
            a.GatewayRefundExpenseAmount, false, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.Refund, refundRecordId, LedgerAccountLine.ActualRefundExpense,
            a.ServiceRefundAmount, false, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.Refund, refundRecordId, LedgerAccountLine.ProviderRecoveryReceivable,
            a.ProviderRecoveryAmount, false, cur, tx.Id, provider, customer, occurred, ct);
        await PostAsync(LedgerSourceType.Refund, refundRecordId, LedgerAccountLine.PlatformAdvancedRefundAmount,
            a.PlatformAdvancedRefundAmount, false, cur, tx.Id, provider, customer, occurred, ct);

        _logger.LogInformation("Ledger posted (refund) for record {RecId} tx {TxId}.", refundRecordId, tx.Id);
    }

    // ── Chargeback (P10) ────────────────────────────────────────────────────────

    public async Task PostChargebackAsync(ChargebackRecordEntity c, long? providerProfileId, CancellationToken ct)
    {
        var occurred = c.ReceivedAtUtc == default ? DateTime.UtcNow : c.ReceivedAtUtc;
        await PostAsync(LedgerSourceType.Chargeback, c.Id, LedgerAccountLine.ChargebackExpense,
            c.ChargebackExpenseAmount, false, c.CurrencyCode, c.PaymentTransactionId, providerProfileId, null, occurred, ct);
        await PostAsync(LedgerSourceType.Chargeback, c.Id, LedgerAccountLine.ActualRefundExpense,
            c.Amount, false, c.CurrencyCode, c.PaymentTransactionId, providerProfileId, null, occurred, ct);
        await PostAsync(LedgerSourceType.Chargeback, c.Id, LedgerAccountLine.ProviderRecoveryReceivable,
            c.ProviderRecoveredAmount, false, c.CurrencyCode, c.PaymentTransactionId, providerProfileId, null, occurred, ct);

        _logger.LogInformation("Ledger posted (chargeback) for record {Id} tx {TxId}.", c.Id, c.PaymentTransactionId);
    }

    // ── Premium (P11) ───────────────────────────────────────────────────────────

    /// <summary>§15 — premium revenue = purchase.UnitPriceSnapshot (SourceType PremiumPurchase, SourceRef purchase id).</summary>
    public async Task PostPremiumPaidAsync(PremiumPurchaseEntity purchase, long? transactionId, CancellationToken ct)
    {
        await PostAsync(LedgerSourceType.PremiumPurchase, purchase.Id, LedgerAccountLine.PremiumProductRevenue,
            purchase.UnitPriceSnapshot, false, purchase.CurrencyCodeSnapshot, transactionId, purchase.ProviderProfileId, null,
            DateTime.UtcNow, ct);
        _logger.LogInformation("Ledger posted (premium paid) for purchase {Code}.", purchase.PurchaseCode);
    }

    /// <summary>§15 — premium refund reverses PremiumProductRevenue. Keyed by (Refund, purchase id) so the runtime refund and
    /// a later backfill are idempotent with each other (the non-reversal revenue lives under SourceType PremiumPurchase).</summary>
    public async Task PostPremiumRefundAsync(PremiumPurchaseEntity purchase, long? transactionId, CancellationToken ct)
    {
        await PostAsync(LedgerSourceType.Refund, purchase.Id, LedgerAccountLine.PremiumProductRevenue,
            purchase.UnitPriceSnapshot, true, purchase.CurrencyCodeSnapshot, transactionId, purchase.ProviderProfileId, null,
            DateTime.UtcNow, ct);
        _logger.LogInformation("Ledger posted (premium refund) for purchase {Code}.", purchase.PurchaseCode);
    }

    // ── Subscription ────────────────────────────────────────────────────────────

    /// <summary>§15/§19.17 — subscription revenue posted on the <b>kind-specific</b> plan line (ProviderPlanRevenue vs
    /// CustomerPlanRevenue) — kept separate, never merged into one line. Provider and participant subscription ids are
    /// independent sequences that share <see cref="LedgerSourceType.Subscription"/>, so the distinct account line (4 vs 5)
    /// is what keeps their keys unique; posting a shared umbrella line here would both collide AND double-count.</summary>
    public async Task PostSubscriptionAsync(
        long subscriptionId, decimal paidAmount, string currency, bool isProvider,
        long profileId, long? transactionId, DateTime occurredAtUtc, CancellationToken ct)
    {
        await PostAsync(LedgerSourceType.Subscription, subscriptionId,
            isProvider ? LedgerAccountLine.ProviderPlanRevenue : LedgerAccountLine.CustomerPlanRevenue,
            paidAmount, false, currency, transactionId, isProvider ? profileId : null, isProvider ? null : profileId, occurredAtUtc, ct);
        _logger.LogInformation("Ledger posted (subscription {Kind}) for {Id}.", isProvider ? "provider" : "customer", subscriptionId);
    }

    // ── Core append (idempotent; skips zero amounts) ────────────────────────────

    private async Task PostAsync(
        LedgerSourceType sourceType, long sourceRef, LedgerAccountLine line, decimal amount, bool isReversal,
        string currency, long? transactionId, long? providerProfileId, long? customerProfileId,
        DateTime occurredAtUtc, CancellationToken ct)
    {
        var rounded = MoneyMath.Round(amount);
        if (rounded <= 0m) return;   // nothing to record for a zero line (keeps the ledger clean + reconcilable)

        var entryCode = GenerateEntryCode(sourceType, sourceRef, line, isReversal);
        if (!_pending.Add(entryCode))
            return;                   // already staged in this unit of work (backfill posting many sources before one save)

        if (await _ledger.ExistsAsync(sourceType, sourceRef, line, isReversal, ct))
            return;                   // idempotent — already committed (duplicate webhook / backfill re-run)

        var entry = FinancialLedgerEntryEntity.Create(
            entryCode:         entryCode,
            accountLine:       line,
            amount:            rounded,
            isReversal:        isReversal,
            currencyCode:      currency,
            sourceType:        sourceType,
            sourceRef:         sourceRef,
            transactionId:     transactionId,
            providerProfileId: providerProfileId == 0 ? null : providerProfileId,
            customerProfileId: customerProfileId == 0 ? null : customerProfileId,
            occurredAtUtc:     occurredAtUtc,
            postedAtUtc:       DateTime.UtcNow);
        await _ledger.AddAsync(entry, ct);
    }

    /// <summary>Deterministic, collision-free entry code (mirrors the idempotency key) — stable across a backfill re-run.</summary>
    private static string GenerateEntryCode(LedgerSourceType sourceType, long sourceRef, LedgerAccountLine line, bool isReversal)
        => $"LED-{(int)sourceType}-{sourceRef}-{(int)line}{(isReversal ? "-R" : "")}";
}
