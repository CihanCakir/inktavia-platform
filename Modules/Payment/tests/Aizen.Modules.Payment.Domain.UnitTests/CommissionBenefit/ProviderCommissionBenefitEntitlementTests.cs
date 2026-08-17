using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.CommissionBenefit;

public sealed class ProviderCommissionBenefitEntitlementTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Now  = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ProviderCommissionBenefitEntitlementEntity Entitlement(long? usageLimit = null, decimal? maxGmv = null)
    {
        var e = ProviderCommissionBenefitEntitlementEntity.Grant("PCE-1", 7, 1, From, null, usageLimit, maxGmv);
        e.Id = 100;
        return e;
    }

    // ── Reserve / Consume / Release + counters ───────────────────────────────────

    [Fact]
    public void Reserve_Then_Consume_Tracks_Usage_And_Gmv()
    {
        var e = Entitlement(usageLimit: 3, maxGmv: 1000m);
        var u = e.Reserve(400m, 4m, "offer-1", Now);
        e.ReservedGMV.Should().Be(400m);
        e.ReservedCount.Should().Be(1);
        e.Version.Should().Be(1);

        e.Consume(u, Now);
        e.ConsumedGMV.Should().Be(400m);
        e.UsedCount.Should().Be(1);
        e.ReservedGMV.Should().Be(0m);
        e.ReservedCount.Should().Be(0);
        u.Status.Should().Be(CustomerBenefitReservationStatus.Consumed);
    }

    [Fact]
    public void Release_Returns_Reserved_Gmv()
    {
        var e = Entitlement(usageLimit: 3, maxGmv: 1000m);
        var u = e.Reserve(400m, 4m, "offer-1", Now);
        e.Release(u, Now);
        e.ReservedGMV.Should().Be(0m);
        e.ReservedCount.Should().Be(0);
        u.Status.Should().Be(CustomerBenefitReservationStatus.Released);
    }

    [Fact]
    public void Reserve_Over_UsageLimit_Is_Exhausted()
    {
        var e = Entitlement(usageLimit: 1);
        e.Reserve(10m, 1m, "offer-1", Now);   // uses the only slot
        Action act = () => e.Reserve(10m, 1m, "offer-2", Now);
        act.Should().Throw<AizenBusinessException>()
           .Where(x => x.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitExhausted);
    }

    [Fact]
    public void Reserve_Over_Gmv_Limit_Is_Exhausted()
    {
        var e = Entitlement(maxGmv: 500m);
        Action act = () => e.Reserve(600m, 6m, "offer-1", Now);
        act.Should().Throw<AizenBusinessException>()
           .Where(x => x.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitExhausted);
    }

    [Fact]
    public void Entitlement_Becomes_Exhausted_When_Usage_Limit_Consumed()
    {
        var e = Entitlement(usageLimit: 1, maxGmv: 1000m);
        var u = e.Reserve(100m, 1m, "offer-1", Now);
        e.Consume(u, Now);
        e.Status.Should().Be(ProviderCommissionBenefitEntitlementStatus.Exhausted);
    }

    [Fact]
    public void Consume_Twice_Is_Guarded()
    {
        var e = Entitlement(usageLimit: 3);
        var u = e.Reserve(100m, 1m, "offer-1", Now);
        e.Consume(u, Now);
        Action second = () => e.Consume(u, Now);
        second.Should().Throw<AizenBusinessException>()
           .Where(x => x.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitUsageInvalidState);
    }

    // ── Rule validation ──────────────────────────────────────────────────────────

    [Fact]
    public void Positive_Adjustment_Surcharge_Is_Rejected()
    {
        Action act = () => ProviderCommissionBenefitRuleEntity.Create(
            "PCB", null, null, null, null, 0.01m, 0m, null, null, null, true, false,
            CommissionRulePriority.Standard, From, null, "TRY");
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitRuleInvalid);
    }

    [Fact]
    public void Exclusive_And_Stackable_Together_Is_Rejected()
    {
        Action act = () => ProviderCommissionBenefitRuleEntity.Create(
            "PCB", null, null, null, null, -0.01m, 0m, null, null, null, true, true,
            CommissionRulePriority.Standard, From, null, "TRY");
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitRuleInvalid);
    }

    [Fact]
    public void Minimum_Commission_Rate_Out_Of_Range_Is_Rejected()
    {
        Action act = () => ProviderCommissionBenefitRuleEntity.Create(
            "PCB", null, null, null, null, -0.01m, 1.5m, null, null, null, true, false,
            CommissionRulePriority.Standard, From, null, "TRY");
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitRuleInvalid);
    }
}
