using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.PlatformFee;

public sealed class PlatformFeeRuleEntityTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PlatformFeeRuleEntity Create(
        PlatformFeeModel model, decimal? rate = null, decimal? fixedAmount = null,
        decimal? min = null, decimal? max = null)
        => PlatformFeeRuleEntity.Create(model, rate, fixedAmount, min, max, "TRY", null, null,
            CommissionRulePriority.Standard, From, null, "PFR");

    [Fact]
    public void Waived_Requires_No_Parameters()
    {
        var rule = Create(PlatformFeeModel.Waived);
        rule.Model.Should().Be(PlatformFeeModel.Waived);
        rule.Status.Should().Be(CommissionRuleStatus.Active);
    }

    [Fact]
    public void PercentageWithBounds_Valid_Is_Created()
    {
        var rule = Create(PlatformFeeModel.PercentageWithBounds, rate: 0.025m, min: 99m, max: 1500m);
        rule.MinAmount.Should().Be(99m);
        rule.MaxAmount.Should().Be(1500m);
    }

    [Theory]
    [InlineData(PlatformFeeModel.Percentage)]                 // missing Rate
    [InlineData(PlatformFeeModel.Fixed)]                      // missing FixedAmount
    [InlineData(PlatformFeeModel.PercentageWithBounds)]       // missing Rate/Min/Max
    public void Model_Incoherent_Throws_Invalid(PlatformFeeModel model)
    {
        Action act = () => Create(model);
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.PlatformFeeRuleInvalid);
    }

    [Fact]
    public void Bounds_With_Min_Greater_Than_Max_Throws()
    {
        Action act = () => Create(PlatformFeeModel.PercentageWithBounds, rate: 0.025m, min: 1500m, max: 99m);
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.PlatformFeeRuleInvalid);
    }
}
