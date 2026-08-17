using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.CustomerBenefit;

public sealed class CustomerBenefitBudgetTests
{
    private static readonly DateTime P0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime P1 = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Now = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    private static CustomerBenefitBudgetEntity Budget(decimal funded = 100m)
    {
        var b = CustomerBenefitBudgetEntity.Create(1, 2, P0, P1, funded, "TRY");
        b.Id = 10;
        return b;
    }

    [Fact]
    public void Reserve_Decrements_Remaining_And_Bumps_Version()
    {
        var b = Budget(100m);
        var r = b.Reserve(30m, "offer-1", Now);

        b.ReservedAmount.Should().Be(30m);
        b.RemainingAmount.Should().Be(70m);
        b.Version.Should().Be(1);
        r.Status.Should().Be(CustomerBenefitReservationStatus.Reserved);
        r.BudgetId.Should().Be(10);
    }

    [Fact]
    public void Consume_Moves_Reserved_To_Consumed()
    {
        var b = Budget(100m);
        var r = b.Reserve(30m, "offer-1", Now);
        b.Consume(r, Now);

        b.ReservedAmount.Should().Be(0m);
        b.ConsumedAmount.Should().Be(30m);
        b.RemainingAmount.Should().Be(70m);
        r.Status.Should().Be(CustomerBenefitReservationStatus.Consumed);
    }

    [Fact]
    public void Release_Returns_Reserved_To_Remaining()
    {
        var b = Budget(100m);
        var r = b.Reserve(30m, "offer-1", Now);
        b.Release(r, Now);

        b.ReservedAmount.Should().Be(0m);
        b.RemainingAmount.Should().Be(100m);
        r.Status.Should().Be(CustomerBenefitReservationStatus.Released);
    }

    [Fact]
    public void Reserve_Above_Remaining_Is_Rejected()
    {
        var b = Budget(100m);
        Action act = () => b.Reserve(150m, "offer-1", Now);
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CustomerBenefitInsufficientRemaining);
    }

    [Fact]
    public void Budget_Is_Capped_At_FundedAmount_Not_Unlimited()
    {
        // "ELITE ≠ unlimited": even a top-tier budget is a finite FundedAmount; reserving beyond it fails.
        var b = Budget(50m);
        b.Reserve(50m, "offer-1", Now);   // exactly the cap
        b.RemainingAmount.Should().Be(0m);
        Action act = () => b.Reserve(0.01m, "offer-2", Now);
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CustomerBenefitInsufficientRemaining);
    }

    [Fact]
    public void Consume_Twice_Is_Guarded_By_Reservation_State()
    {
        var b = Budget(100m);
        var r = b.Reserve(30m, "offer-1", Now);
        b.Consume(r, Now);
        Action second = () => b.Consume(r, Now);   // reservation no longer Reserved
        second.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CustomerBenefitReservationInvalidState);
    }
}
