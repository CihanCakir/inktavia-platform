using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.ProfitProtection;

public sealed class ProfitProtectionPolicyEntityTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ProfitProtectionPolicyEntity Create(
        decimal minTxnRate = 0m, decimal custVarShare = 0.5m, DateTime? to = null)
        => ProfitProtectionPolicyEntity.Create(
            "TRY", 0m, 0m, 0m, 0m, 0m, minTxnRate, 0m, 0m, 0m, 0m, 0m, custVarShare,
            ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit, From, to, "PPOL-1");

    [Fact]
    public void Valid_Create_Succeeds()
    {
        var p = Create();
        p.CurrencyCode.Should().Be("TRY");
        p.Status.Should().Be(CommissionRuleStatus.Active);
    }

    [Fact]
    public void Negative_Rate_Throws_Invalid()
    {
        Action act = () => Create(minTxnRate: -0.01m);
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProfitProtectionPolicyInvalid);
    }

    [Fact]
    public void Share_Outside_Unit_Interval_Throws_Invalid()
    {
        Action act = () => Create(custVarShare: 1.5m);
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProfitProtectionPolicyInvalid);
    }

    [Fact]
    public void EffectiveTo_Not_After_From_Throws()
    {
        Action act = () => Create(to: From);
        act.Should().Throw<AizenBusinessException>();
    }
}
