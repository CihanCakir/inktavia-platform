using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Plan;

public sealed class ProviderPlanPriceEntityTests
{
    private static readonly DateTime From = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Valid_Sets_Fields()
    {
        var e = ProviderPlanPriceEntity.Create(
            2, ProviderPlanPriceType.Launch, BillingPeriod.Monthly, 499m, "try", From, From.AddMonths(6), "P1");
        e.PriceAmount.Should().Be(499m);
        e.CurrencyCode.Should().Be("TRY");
        e.BillingPeriod.Should().Be(BillingPeriod.Monthly);
    }

    [Fact]
    public void Negative_Price_Throws()
    {
        Action act = () => ProviderPlanPriceEntity.Create(
            2, ProviderPlanPriceType.List, BillingPeriod.Monthly, -1m, "TRY", From, null, "P");
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderPlanPriceConflict);
    }

    [Fact]
    public void EffectiveTo_Not_After_From_Throws()
    {
        Action act = () => ProviderPlanPriceEntity.Create(
            2, ProviderPlanPriceType.List, BillingPeriod.Monthly, 499m, "TRY", From, From, "P");
        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void CoversInstant_Is_Half_Open()
    {
        var e = ProviderPlanPriceEntity.Create(
            2, ProviderPlanPriceType.Launch, BillingPeriod.Monthly, 499m, "TRY", From, From.AddMonths(6), "P");
        e.CoversInstant(From).Should().BeTrue();                    // lower bound inclusive
        e.CoversInstant(From.AddMonths(6)).Should().BeFalse();      // upper bound exclusive
        e.CoversInstant(From.AddMonths(6).AddTicks(-1)).Should().BeTrue();
    }
}
