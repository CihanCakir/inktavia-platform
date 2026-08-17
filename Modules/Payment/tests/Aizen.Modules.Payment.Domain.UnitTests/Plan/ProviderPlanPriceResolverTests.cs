using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Plan;

public sealed class ProviderPlanPriceResolverTests
{
    private static readonly DateTime GoLive    = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LaunchEnd = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);  // GoLive + 6 months

    private static ProviderPlanPriceEntity Price(
        decimal amount, DateTime from, DateTime? to, long id = 1, long planId = 2)
    {
        var e = ProviderPlanPriceEntity.Create(
            planId, ProviderPlanPriceType.List, BillingPeriod.Monthly, amount, "TRY", from, to, $"P{id}");
        e.Id = id;
        return e;
    }

    private static List<ProviderPlanPriceEntity> LaunchThenList() => new()
    {
        Price(499m,  GoLive,    LaunchEnd, id: 1),   // Launch
        Price(1490m, LaunchEnd, null,      id: 2),   // List
    };

    // ── Point-in-time resolution + boundary ──────────────────────────────────────

    [Fact]
    public void Resolve_In_Launch_Window_Returns_Launch_Price()
    {
        var at = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        ProviderPlanPriceResolver.Resolve(LaunchThenList(), at)!.PriceAmount.Should().Be(499m);
    }

    [Fact]
    public void Resolve_At_Launch_End_Boundary_Returns_List_Price_UpperExclusive()
    {
        // The upper bound belongs to the next record (half-open) → List wins at exactly LaunchEnd.
        ProviderPlanPriceResolver.Resolve(LaunchThenList(), LaunchEnd)!.PriceAmount.Should().Be(1490m);
    }

    [Fact]
    public void Resolve_One_Instant_Before_LaunchEnd_Still_Launch()
    {
        var at = LaunchEnd.AddTicks(-1);
        ProviderPlanPriceResolver.Resolve(LaunchThenList(), at)!.PriceAmount.Should().Be(499m);
    }

    [Fact]
    public void Resolve_Before_GoLive_Returns_Null()
    {
        var at = GoLive.AddDays(-1);
        ProviderPlanPriceResolver.Resolve(LaunchThenList(), at).Should().BeNull();
    }

    [Fact]
    public void Free_Single_Open_Row_Resolves_Zero_Any_Time()
    {
        var free = new List<ProviderPlanPriceEntity> { Price(0m, GoLive, null, id: 9, planId: 1) };
        ProviderPlanPriceResolver.Resolve(free, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc))!
            .PriceAmount.Should().Be(0m);
    }

    // ── Overlap = configuration error at resolve ─────────────────────────────────

    [Fact]
    public void Resolve_Throws_Conflict_When_Two_Prices_Cover_The_Instant()
    {
        var overlapping = new List<ProviderPlanPriceEntity>
        {
            Price(499m,  GoLive,               LaunchEnd,             id: 1),
            Price(600m,  GoLive.AddMonths(1),  LaunchEnd.AddMonths(1), id: 2),  // overlaps the first
        };
        var at = GoLive.AddMonths(2);

        Action act = () => ProviderPlanPriceResolver.Resolve(overlapping, at);

        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderPlanPriceConflict);
    }

    // ── Create/Update overlap+gap guard (§4/§13.1) ───────────────────────────────

    [Fact]
    public void Guard_First_Insert_Is_Ok()
    {
        var candidate = Price(499m, GoLive, LaunchEnd, id: 0);
        ProviderPlanPriceResolver.ValidateInsertable(candidate, Array.Empty<ProviderPlanPriceEntity>())
            .Outcome.Should().Be(PlanPriceGuardOutcome.Ok);
    }

    [Fact]
    public void Guard_Contiguous_List_After_Launch_Is_Ok()
    {
        var existing  = new[] { Price(499m, GoLive, LaunchEnd, id: 1) };
        var candidate = Price(1490m, LaunchEnd, null, id: 0);   // starts exactly where launch ends
        ProviderPlanPriceResolver.ValidateInsertable(candidate, existing)
            .Outcome.Should().Be(PlanPriceGuardOutcome.Ok);
    }

    [Fact]
    public void Guard_Overlapping_Range_Is_Rejected()
    {
        var existing  = new[] { Price(499m, GoLive, LaunchEnd, id: 1) };
        var candidate = Price(1490m, GoLive.AddMonths(3), LaunchEnd.AddMonths(3), id: 0); // overlaps launch
        var result = ProviderPlanPriceResolver.ValidateInsertable(candidate, existing);
        result.Outcome.Should().Be(PlanPriceGuardOutcome.Overlap);
        result.ConflictingId.Should().Be(1);
    }

    [Fact]
    public void Guard_NonContiguous_Range_Is_A_Gap()
    {
        var existing  = new[] { Price(499m, GoLive, LaunchEnd, id: 1) };
        var candidate = Price(1490m, LaunchEnd.AddMonths(1), null, id: 0); // leaves a 1-month hole
        ProviderPlanPriceResolver.ValidateInsertable(candidate, existing)
            .Outcome.Should().Be(PlanPriceGuardOutcome.Gap);
    }

    [Fact]
    public void Guard_Update_Excludes_Self_By_Id()
    {
        var existing  = new[] { Price(499m, GoLive, LaunchEnd, id: 7) };
        var candidate = Price(520m, GoLive, LaunchEnd, id: 7);  // same identity → not a self-conflict
        ProviderPlanPriceResolver.ValidateInsertable(candidate, existing)
            .Outcome.Should().Be(PlanPriceGuardOutcome.Ok);
    }
}
