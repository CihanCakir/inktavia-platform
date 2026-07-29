using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Reporting;

/// <summary>
/// BE-P12 §15/§19.17 — an <b>append-only, immutable</b> reporting ledger line, <b>derived</b> from an immutable source
/// (PaymentEconomicsSnapshot / RefundAllocation / ChargebackRecord / PremiumPurchase / subscription) — never recomputed.
/// <para><b>Amount is always positive</b>; <see cref="Nature"/> + <see cref="IsReversal"/> carry the sign in reports. A
/// correction/reversal is a NEW entry, never a mutation. Idempotent on <c>(SourceType, SourceRef, AccountLine, IsReversal)</c>
/// — re-posting the same source line is a no-op (the unique index is the hard backstop; the posting service pre-checks).</para>
/// </summary>
[DocumentationInfo("Financial ledger entry",
    "Append-only, immutable reporting line derived from an immutable source. Amount positive; nature+IsReversal carry the sign; reversal = new entry.")]
public sealed class FinancialLedgerEntryEntity : AizenEntityWithAudit
{
    public string             EntryCode         { get; private set; } = default!;   // unique
    public LedgerAccountLine  AccountLine       { get; private set; }
    public LedgerEntryNature  Nature            { get; private set; }
    public decimal            Amount            { get; private set; }               // always ≥ 0
    public bool               IsReversal        { get; private set; }               // contra entry (flips the sign)
    public string             CurrencyCode      { get; private set; } = "TRY";
    public LedgerSourceType   SourceType        { get; private set; }
    public long               SourceRef         { get; private set; }
    public long?              TransactionId     { get; private set; }
    public long?              ProviderProfileId { get; private set; }
    public long?              CustomerProfileId { get; private set; }
    public DateTime           OccurredAtUtc     { get; private set; }
    public DateTime           PostedAtUtc       { get; private set; }
    public string?            Note              { get; private set; }

    private FinancialLedgerEntryEntity() { }

    public static FinancialLedgerEntryEntity Create(
        string entryCode, LedgerAccountLine accountLine, decimal amount, bool isReversal,
        string currencyCode, LedgerSourceType sourceType, long sourceRef,
        long? transactionId, long? providerProfileId, long? customerProfileId,
        DateTime occurredAtUtc, DateTime postedAtUtc, string? note = null)
    {
        if (amount < 0m) throw new ArgumentException("Ledger Amount must be ≥ 0 (nature/IsReversal carry the sign).", nameof(amount));

        return new FinancialLedgerEntryEntity
        {
            EntryCode         = entryCode,
            AccountLine       = accountLine,
            Nature            = NatureOf(accountLine),
            Amount            = amount,
            IsReversal        = isReversal,
            CurrencyCode      = currencyCode.ToUpperInvariant(),
            SourceType        = sourceType,
            SourceRef         = sourceRef,
            TransactionId     = transactionId,
            ProviderProfileId = providerProfileId,
            CustomerProfileId = customerProfileId,
            OccurredAtUtc     = occurredAtUtc,
            PostedAtUtc       = postedAtUtc,
            Note              = note,
            IsActive          = true,
        };
    }

    /// <summary>Signed contribution to the Inktavia P&amp;L: +revenue/receivable, −expense; Liability/Memo = 0 (excluded).</summary>
    public decimal ContributionSigned()
    {
        var sign = IsReversal ? -1m : 1m;
        return Nature switch
        {
            LedgerEntryNature.Revenue    => +Amount * sign,
            LedgerEntryNature.Receivable => +Amount * sign,
            LedgerEntryNature.Expense    => -Amount * sign,
            _                            => 0m,   // Liability (VAT) + Memo (provider-funded discount, contributions) excluded
        };
    }

    /// <summary>BE-P12 §15/§19.17 — the fixed accounting nature of each account line (kept separate, no merging).</summary>
    public static LedgerEntryNature NatureOf(LedgerAccountLine line) => line switch
    {
        LedgerAccountLine.ProviderCommissionRevenue     => LedgerEntryNature.Revenue,
        LedgerAccountLine.CustomerPlatformFeeNetRevenue => LedgerEntryNature.Revenue,
        LedgerAccountLine.SubscriptionRevenue           => LedgerEntryNature.Revenue,
        LedgerAccountLine.ProviderPlanRevenue           => LedgerEntryNature.Revenue,
        LedgerAccountLine.CustomerPlanRevenue           => LedgerEntryNature.Revenue,
        LedgerAccountLine.ProviderAddOnRevenue          => LedgerEntryNature.Revenue,
        LedgerAccountLine.PremiumProductRevenue         => LedgerEntryNature.Revenue,

        LedgerAccountLine.CustomerPlatformFeeVatLiability => LedgerEntryNature.Liability,

        LedgerAccountLine.PaymentProcessingExpense              => LedgerEntryNature.Expense,
        LedgerAccountLine.GatewayOtherExpense                   => LedgerEntryNature.Expense,
        LedgerAccountLine.RefundProcessingExpense               => LedgerEntryNature.Expense,
        LedgerAccountLine.ChargebackExpense                     => LedgerEntryNature.Expense,
        LedgerAccountLine.PlatformFundedCustomerDiscountExpense => LedgerEntryNature.Expense,
        LedgerAccountLine.ExpectedPaymentProcessingExpense      => LedgerEntryNature.Expense,
        LedgerAccountLine.ActualPaymentProcessingExpense        => LedgerEntryNature.Expense,
        LedgerAccountLine.RefundRiskReserve                     => LedgerEntryNature.Expense,
        LedgerAccountLine.ActualRefundExpense                   => LedgerEntryNature.Expense,

        LedgerAccountLine.ProviderCommissionBenefitCost => LedgerEntryNature.Expense,

        LedgerAccountLine.ProviderRecoveryReceivable  => LedgerEntryNature.Receivable,
        LedgerAccountLine.PlatformAdvancedRefundAmount => LedgerEntryNature.Receivable,

        // §19.17 — provider-funded customer discount is a MEMO (NOT an Inktavia expense); contributions/gross-share are memos.
        LedgerAccountLine.ProviderFundedCustomerDiscount => LedgerEntryNature.Memo,
        LedgerAccountLine.PlatformGrossShare             => LedgerEntryNature.Memo,
        LedgerAccountLine.PlatformSettlementNetAmount    => LedgerEntryNature.Memo,
        LedgerAccountLine.CustomerSideContribution       => LedgerEntryNature.Memo,
        LedgerAccountLine.ProviderSideContribution       => LedgerEntryNature.Memo,
        LedgerAccountLine.TotalTransactionContribution   => LedgerEntryNature.Memo,
        LedgerAccountLine.NetMarketplaceContribution     => LedgerEntryNature.Memo,

        _ => LedgerEntryNature.Memo,
    };
}
