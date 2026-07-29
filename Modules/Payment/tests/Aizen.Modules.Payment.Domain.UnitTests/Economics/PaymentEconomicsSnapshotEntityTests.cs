using System.Reflection;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Money;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

public sealed class PaymentEconomicsSnapshotEntityTests
{
    // ── Canonical, fully-consistent economic input set (TRY, 20% VAT) ───────────
    //   Service net 1000, VAT 200, gross 1200. Commission 15% → 150. ProviderNet 850.
    //   Platform fee net 100, VAT 20, gross 120. CustomerPayable = service = 1000.
    //   CustomerTotal = 1000 + 120 = 1120. PlatformGrossShare = 150 + 120 = 270.
    private static PaymentEconomicsSnapshotEntity CreateValid(
        decimal  serviceAmount                = 1000.00m,
        decimal  serviceVatAmount             = 200.00m,
        decimal  serviceGrossAmount           = 1200.00m,
        decimal  customerPayableServiceAmount = 1000.00m,
        decimal  commissionBaseAmount         = 1000.00m,
        decimal  commissionRate               = 0.1500m,
        decimal  commissionAmount             = 150.00m,
        long?    platformFeeRuleId            = null,
        decimal  platformFeeBaseAmount        = 1000.00m,
        decimal  platformFeeRate              = 0.0250m,
        decimal  platformFeeMinimum           = 99.00m,
        decimal  platformFeeMaximum           = 1500.00m,
        decimal  platformFeeNetAmount         = 100.00m,
        decimal  platformFeeVatAmount         = 20.00m,
        decimal  platformFeeGrossAmount       = 120.00m,
        decimal  customerTotalAmount          = 1120.00m,
        string?  snapshotCode                 = "PES-20260728-TEST",
        DateTime? createdAtUtc                = null)
        => PaymentEconomicsSnapshotEntity.Create(
            TransactionContextType.ServiceRequest, contextId: 42,
            serviceAmount, serviceVatAmount, serviceGrossAmount, customerPayableServiceAmount,
            commissionBaseAmount, commissionRate, commissionAmount,
            platformFeeRuleId, platformFeeBaseAmount, platformFeeRate,
            platformFeeMinimum, platformFeeMaximum,
            platformFeeNetAmount, platformFeeVatAmount, platformFeeGrossAmount,
            customerTotalAmount,
            snapshotCode, createdAtUtc ?? new DateTime(2026, 7, 28, 9, 0, 0, DateTimeKind.Utc));

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_HappyPath_ComputesDerivedAmounts_AndSetsMetadata()
    {
        var s = CreateValid();

        s.ServiceGrossAmountSnapshot.Should().Be(1200.00m);
        s.CommissionAmountSnapshot.Should().Be(150.00m);
        s.ProviderNetAmountSnapshot.Should().Be(850.00m);          // derived: 1000 - 150
        s.PlatformFeeGrossAmountSnapshot.Should().Be(120.00m);
        s.CustomerTotalAmountSnapshot.Should().Be(1120.00m);
        s.PlatformGrossShareSnapshot.Should().Be(270.00m);          // derived: 150 + 120

        s.SnapshotCode.Should().Be("PES-20260728-TEST");
        s.CreatedAtUtc.Should().Be(new DateTime(2026, 7, 28, 9, 0, 0, DateTimeKind.Utc));
        s.CurrencyCodeSnapshot.Should().Be("TRY");
        s.RoundingModeSnapshot.Should().Be("AwayFromZero-2");
        s.ContextType.Should().Be(TransactionContextType.ServiceRequest);
        s.ContextId.Should().Be(42);
    }

    [Fact]
    public void Create_GeneratesSnapshotCode_WhenNotSupplied()
    {
        var s = CreateValid(snapshotCode: null, createdAtUtc: new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc));
        s.SnapshotCode.Should().StartWith("PES-20260728-");
        s.SnapshotCode.Length.Should().Be("PES-20260728-".Length + 4);
    }

    // ── Reconstruction to the kuruş (§13.10) ────────────────────────────────────

    [Fact]
    public void Reconstruction_ProviderNet_Plus_PlatformGrossShare_Equals_CustomerTotal_Exactly()
    {
        var s = CreateValid();
        (s.ProviderNetAmountSnapshot + s.PlatformGrossShareSnapshot)
            .Should().Be(s.CustomerTotalAmountSnapshot);

        // and the platform book share equals commission + platform-fee-gross
        s.PlatformGrossShareSnapshot
            .Should().Be(s.CommissionAmountSnapshot + s.PlatformFeeGrossAmountSnapshot);
    }

    // ── Rounding: commission & fee rounded SEPARATELY; net derived; midpoint away ─

    [Fact]
    public void Rounding_Commission_And_Fee_Rounded_Separately_Net_Derived()
    {
        // base 333.33 * 0.1500 = 49.9995 → 50.00 (away-from-zero on the .5 midpoint of the 3rd dp)
        // service 333.33, VAT 66.67, gross 400.00. providerNet = 333.33 - 50.00 = 283.33.
        // fee net 8.335 → 8.34, fee vat 1.665 → 1.67, fee gross = 8.34 + 1.67 = 10.01 (each rounded separately).
        // customerPayable = 333.33. customerTotal = 333.33 + 10.01 = 343.34.
        var s = CreateValid(
            serviceAmount: 333.33m, serviceVatAmount: 66.67m, serviceGrossAmount: 400.00m,
            customerPayableServiceAmount: 333.33m,
            commissionBaseAmount: 333.33m, commissionRate: 0.1500m,
            commissionAmount: MoneyMath.Round(333.33m * 0.1500m),   // 50.00
            platformFeeNetAmount: 8.335m, platformFeeVatAmount: 1.665m,
            platformFeeGrossAmount: MoneyMath.Round(8.335m) + MoneyMath.Round(1.665m), // 8.34 + 1.67 = 10.01
            customerTotalAmount: 333.33m + (MoneyMath.Round(8.335m) + MoneyMath.Round(1.665m)));

        s.CommissionAmountSnapshot.Should().Be(50.00m);
        s.ProviderNetAmountSnapshot.Should().Be(283.33m);                    // derived, exact
        s.PlatformFeeGrossAmountSnapshot.Should().Be(10.01m);                // 8.34 + 1.67, NOT round(10.00)
        (s.ProviderNetAmountSnapshot + s.PlatformGrossShareSnapshot)
            .Should().Be(s.CustomerTotalAmountSnapshot);                     // reconstruction to the kuruş
    }

    // ── VAT: gross == net + vat for service and platform fee (20% VAT) ──────────

    [Fact]
    public void Vat_ServiceGross_And_PlatformFeeGross_Equal_Net_Plus_Vat()
    {
        var s = CreateValid();
        s.ServiceGrossAmountSnapshot.Should().Be(s.ServiceAmountSnapshot + s.ServiceVatAmountSnapshot);
        s.PlatformFeeGrossAmountSnapshot.Should().Be(s.PlatformFeeNetAmountSnapshot + s.PlatformFeeVatAmountSnapshot);
    }

    // ── Invariant violations throw — ZERO tolerance (0.01 mismatch) ─────────────

    [Fact]
    public void Invariant_ServiceGross_Mismatch_Throws()
    {
        Action act = () => CreateValid(serviceGrossAmount: 1200.01m); // != 1000 + 200
        act.Should().Throw<PaymentEconomicsInvariantException>()
           .WithMessage("*ServiceGrossAmount == ServiceAmount + ServiceVatAmount*");
    }

    [Fact]
    public void Invariant_PlatformFeeGross_Mismatch_Throws()
    {
        Action act = () => CreateValid(
            platformFeeGrossAmount: 120.01m,          // != 100 + 20
            customerTotalAmount: 1120.01m);           // keep §7 consistent so §3 is the one that trips
        act.Should().Throw<PaymentEconomicsInvariantException>()
           .WithMessage("*PlatformFeeGrossAmount == PlatformFeeNetAmount + PlatformFeeVatAmount*");
    }

    [Fact]
    public void Invariant_Commission_Not_Equal_BaseTimesRate_Throws()
    {
        Action act = () => CreateValid(commissionAmount: 150.01m); // != round(1000 * 0.15) = 150.00
        act.Should().Throw<PaymentEconomicsInvariantException>()
           .WithMessage("*CommissionAmount == MoneyMath.Round(CommissionBaseAmount * CommissionRate)*");
    }

    [Fact]
    public void Invariant_CustomerTotal_Not_Equal_Payable_Plus_FeeGross_Throws()
    {
        // Keep §1 satisfied (providerNet 850 + grossShare 270 = 1120 = customerTotal) but break §7 by making
        // payable (1010) differ from service so payable + feeGross (1130) != customerTotal (1120).
        Action act = () => CreateValid(
            customerPayableServiceAmount: 1010.00m,
            customerTotalAmount: 1120.00m);
        act.Should().Throw<PaymentEconomicsInvariantException>()
           .WithMessage("*CustomerTotalAmount == CustomerPayableServiceAmount + PlatformFeeGrossAmount*");
    }

    [Fact]
    public void Invariant_ProviderNet_Plus_GrossShare_Not_Equal_CustomerTotal_Throws()
    {
        // Break §1 without breaking §7: shift customerPayable so customerTotal (payable+feeGross) holds,
        // but providerNet + (commission+feeGross) no longer reconciles.
        // payable 900 → customerTotal must be 900 + 120 = 1020 for §7.
        // §1: providerNet 850 + platformGrossShare 270 = 1120 != 1020 → throws.
        Action act = () => CreateValid(
            customerPayableServiceAmount: 900.00m,
            customerTotalAmount: 1020.00m);
        act.Should().Throw<PaymentEconomicsInvariantException>()
           .WithMessage("*ProviderNetAmount + PlatformGrossShare == CustomerTotalAmount*");
    }

    [Fact]
    public void Invariant_CustomerTotal_Below_ProviderNet_Throws()
    {
        // Negative platform fee construction: fee net -300, vat 0, gross -300 (self-consistent §3).
        // service 1000, commission 0 → providerNet 1000. payable 1000.
        // customerTotal = 1000 + (-300) = 700 (§7 ok). platformGrossShare = 0 + (-300) = -300.
        // §1: 1000 + (-300) = 700 == customerTotal ok. §2: 700 >= 1000 FALSE → throws.
        Action act = () => CreateValid(
            commissionRate: 0m, commissionAmount: 0m,
            platformFeeNetAmount: -300.00m, platformFeeVatAmount: 0m, platformFeeGrossAmount: -300.00m,
            customerTotalAmount: 700.00m);
        act.Should().Throw<PaymentEconomicsInvariantException>()
           .WithMessage("*CustomerTotalAmount >= ProviderNetAmount*");
    }

    // ── Immutability: no public setters / no mutator methods post-Create ────────

    [Fact]
    public void Entity_Exposes_No_Public_Setters()
    {
        var settable = typeof(PaymentEconomicsSnapshotEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true })
            .Where(p => p.DeclaringType == typeof(PaymentEconomicsSnapshotEntity))
            .Select(p => p.Name)
            .ToList();

        settable.Should().BeEmpty("the snapshot is immutable — construction is only via Create");
    }

    [Fact]
    public void Entity_Exposes_No_Instance_Mutator_Methods()
    {
        var declaredMethods = typeof(PaymentEconomicsSnapshotEntity)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)   // exclude property getters
            .Select(m => m.Name)
            .ToList();

        declaredMethods.Should().BeEmpty("an immutable ledger record exposes no Update/mutator surface");
    }
}
