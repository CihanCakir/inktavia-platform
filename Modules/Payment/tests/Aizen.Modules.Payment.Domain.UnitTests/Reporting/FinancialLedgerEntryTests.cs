using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Reporting;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Reporting;

/// <summary>
/// BE-P12 §15/§19.17 — the pure ledger entry: fixed nature per account line, positive-amount + sign-via-nature/reversal,
/// and the §19.17 classifications (VAT = Liability not revenue; provider-funded discount = Memo not expense).
/// </summary>
public sealed class FinancialLedgerEntryTests
{
    private static FinancialLedgerEntryEntity Entry(LedgerAccountLine line, decimal amount, bool reversal = false)
        => FinancialLedgerEntryEntity.Create($"E-{line}-{reversal}", line, amount, reversal, "TRY",
            LedgerSourceType.AcceptanceSnapshot, 1, 10, 20, 30, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public void Nature_IsFixedPerLine_PerSpec()
    {
        FinancialLedgerEntryEntity.NatureOf(LedgerAccountLine.ProviderCommissionRevenue).Should().Be(LedgerEntryNature.Revenue);
        FinancialLedgerEntryEntity.NatureOf(LedgerAccountLine.PremiumProductRevenue).Should().Be(LedgerEntryNature.Revenue);
        // §19.17 — VAT is a Liability, NOT revenue.
        FinancialLedgerEntryEntity.NatureOf(LedgerAccountLine.CustomerPlatformFeeVatLiability).Should().Be(LedgerEntryNature.Liability);
        // §19.17 — provider-funded customer discount is a MEMO, NOT an expense.
        FinancialLedgerEntryEntity.NatureOf(LedgerAccountLine.ProviderFundedCustomerDiscount).Should().Be(LedgerEntryNature.Memo);
        // Platform-funded discount IS an expense.
        FinancialLedgerEntryEntity.NatureOf(LedgerAccountLine.PlatformFundedCustomerDiscountExpense).Should().Be(LedgerEntryNature.Expense);
        FinancialLedgerEntryEntity.NatureOf(LedgerAccountLine.ChargebackExpense).Should().Be(LedgerEntryNature.Expense);
    }

    [Fact]
    public void Amount_MustBePositive_SignCarriedByNatureAndReversal()
    {
        var negative = () => Entry(LedgerAccountLine.ProviderCommissionRevenue, -5m);
        negative.Should().Throw<ArgumentException>();

        Entry(LedgerAccountLine.ProviderCommissionRevenue, 100m).ContributionSigned().Should().Be(+100m);   // revenue +
        Entry(LedgerAccountLine.ProviderCommissionRevenue, 100m, reversal: true).ContributionSigned().Should().Be(-100m); // contra −
        Entry(LedgerAccountLine.ChargebackExpense, 40m).ContributionSigned().Should().Be(-40m);            // expense −
        Entry(LedgerAccountLine.CustomerPlatformFeeVatLiability, 18m).ContributionSigned().Should().Be(0m); // liability excluded
        Entry(LedgerAccountLine.ProviderFundedCustomerDiscount, 25m).ContributionSigned().Should().Be(0m);  // memo excluded
    }
}
