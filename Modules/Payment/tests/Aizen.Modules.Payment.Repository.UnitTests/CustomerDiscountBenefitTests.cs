using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using Aizen.Modules.Payment.Repository.Seed;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

public sealed class CustomerDiscountBenefitTests
{
    private static PaymentDbContext NewInMemoryDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"cdb-{Guid.NewGuid():N}").Options);

    // ── Reconciliation seed: plan ServiceDiscountRate → PlatformFunded plan-scoped rule ──

    [Fact]
    public async Task Seed_Reconciles_Plan_ServiceDiscountRate_As_PlatformFunded_Rule()
    {
        await using var db = NewInMemoryDb();
        var gold = ParticipantPlanEntity.Create("GOLD", "Gold", null, 199m, null, null, null, 0.05m, 0.10m, 1.5m, 2);
        db.ParticipantPlans.Add(gold);
        await db.SaveChangesAsync();

        await new CustomerDiscountBenefitSeed(db, NullLogger<CustomerDiscountBenefitSeed>.Instance).SeedAsync();

        var repo = new CustomerDiscountRuleRepository(db);
        var rule = await repo.ResolveAsync(new CustomerDiscountResolveContext(gold.Id, null, "TRY"), DateTime.UtcNow);

        rule.Should().NotBeNull();
        rule!.DiscountType.Should().Be(CustomerDiscountType.Percent);
        rule.DiscountRate.Should().Be(0.05m);
        rule.FundingMode.Should().Be(CustomerDiscountFundingMode.PlatformFunded);

        // and a per-plan benefit budget policy exists
        (await db.CustomerBenefitBudgetPolicies.CountAsync(x => x.CustomerPlanId == gold.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Seed_Is_Idempotent()
    {
        await using var db = NewInMemoryDb();
        db.ParticipantPlans.Add(ParticipantPlanEntity.Create("GOLD", "Gold", null, 199m, null, null, null, 0.05m, 0.10m, 1.5m, 2));
        await db.SaveChangesAsync();
        var seed = new CustomerDiscountBenefitSeed(db, NullLogger<CustomerDiscountBenefitSeed>.Instance);

        await seed.SeedAsync();
        await seed.SeedAsync();

        (await db.CustomerDiscountRules.CountAsync()).Should().Be(1);
        (await db.CustomerBenefitBudgetPolicies.CountAsync()).Should().Be(1);
    }

    // ── Budget service: reserve / consume / release / insufficient / idempotent ──

    private static async Task<(PaymentDbContext db, long budgetId)> SeedBudget(decimal funded)
    {
        var db = NewInMemoryDb();
        var b = CustomerBenefitBudgetEntity.Create(1, 2, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddMonths(1), funded, "TRY");
        db.CustomerBenefitBudgets.Add(b);
        await db.SaveChangesAsync();
        return (db, b.Id);
    }

    [Fact]
    public async Task Reserve_Then_Consume_Adjusts_Budget()
    {
        var (db, budgetId) = await SeedBudget(100m);
        await using var _ = db;
        var svc = new CustomerBenefitBudgetService(new CustomerBenefitBudgetRepository(db));

        var reservation = await svc.ReserveAsync(budgetId, 30m, "offer-1");
        (await db.CustomerBenefitBudgets.AsNoTracking().FirstAsync(x => x.Id == budgetId)).RemainingAmount.Should().Be(70m);

        await svc.ConsumeAsync(reservation.Id);
        var after = await db.CustomerBenefitBudgets.AsNoTracking().FirstAsync(x => x.Id == budgetId);
        after.ConsumedAmount.Should().Be(30m);
        after.ReservedAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Reserve_Above_Remaining_Is_Rejected()
    {
        var (db, budgetId) = await SeedBudget(100m);
        await using var _ = db;
        var svc = new CustomerBenefitBudgetService(new CustomerBenefitBudgetRepository(db));

        Func<Task> act = () => svc.ReserveAsync(budgetId, 150m, "offer-1");
        await act.Should().ThrowAsync<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CustomerBenefitInsufficientRemaining);
    }

    [Fact]
    public async Task Duplicate_Consume_Is_Idempotent()
    {
        var (db, budgetId) = await SeedBudget(100m);
        await using var _ = db;
        var svc = new CustomerBenefitBudgetService(new CustomerBenefitBudgetRepository(db));

        var reservation = await svc.ReserveAsync(budgetId, 30m, "offer-1");
        await svc.ConsumeAsync(reservation.Id);
        await svc.ConsumeAsync(reservation.Id);   // duplicate webhook — must be a no-op

        var after = await db.CustomerBenefitBudgets.AsNoTracking().FirstAsync(x => x.Id == budgetId);
        after.ConsumedAmount.Should().Be(30m, "a duplicate consume must not double-spend");
    }

    [Fact]
    public async Task Reserve_Is_Idempotent_Per_ContextRef()
    {
        var (db, budgetId) = await SeedBudget(100m);
        await using var _ = db;
        var svc = new CustomerBenefitBudgetService(new CustomerBenefitBudgetRepository(db));

        var r1 = await svc.ReserveAsync(budgetId, 30m, "offer-1");
        var r2 = await svc.ReserveAsync(budgetId, 30m, "offer-1");   // same context — returns the same reservation

        r2.Id.Should().Be(r1.Id);
        (await db.CustomerBenefitBudgets.AsNoTracking().FirstAsync(x => x.Id == budgetId)).ReservedAmount.Should().Be(30m);
    }

    // ── Concurrent reserve on the same budget → exactly one wins (SQLite relational) ──

    [Fact]
    public async Task Concurrent_Reserve_On_Same_Budget_One_Wins()
    {
        // A shared relational store (SQLite) enforces the optimistic-concurrency Version token. Only the budget +
        // reservation tables are created (the full Npgsql-shaped schema isn't SQLite-portable), which is all this needs.
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<PaymentDbContext>().UseSqlite(connection).Options;
            await CreateBenefitTablesAsync(connection);

            long budgetId;
            await using (var setup = new PaymentDbContext(options))
            {
                var b = CustomerBenefitBudgetEntity.Create(1, 2, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddMonths(1), 100m, "TRY");
                setup.CustomerBenefitBudgets.Add(b);
                await setup.SaveChangesAsync();
                budgetId = b.Id;
            }

            // Both contexts load the SAME budget snapshot (Version 0) BEFORE either commits — a real race.
            await using var dbA = new PaymentDbContext(options);
            await using var dbB = new PaymentDbContext(options);
            var repoA = new CustomerBenefitBudgetRepository(dbA);
            var repoB = new CustomerBenefitBudgetRepository(dbB);

            var budgetA = await repoA.GetByIdAsync(budgetId);   // Version 0
            var budgetB = await repoB.GetByIdAsync(budgetId);   // Version 0 (stale once A commits)

            await repoA.AddReservationAsync(budgetA!.Reserve(60m, "offer-A", DateTime.UtcNow));
            await repoA.SaveChangesConcurrencySafeAsync();       // first wins → Version 1

            await repoB.AddReservationAsync(budgetB!.Reserve(60m, "offer-B", DateTime.UtcNow));
            Func<Task> second = () => repoB.SaveChangesConcurrencySafeAsync();  // WHERE Version=0 → 0 rows
            await second.Should().ThrowAsync<AizenBusinessException>()
               .Where(e => e.ErrorCode == (int)PaymentErrorCode.CustomerBenefitConcurrencyConflict);

            // Only the first reservation stuck; no double-spend.
            await using var verify = new PaymentDbContext(options);
            (await verify.CustomerBenefitBudgets.AsNoTracking().FirstAsync(x => x.Id == budgetId)).ReservedAmount.Should().Be(60m);
        }
        finally
        {
            connection.Close();
        }
    }

    /// <summary>Creates just the two benefit tables this concurrency test needs (schema "payment" is ignored by SQLite).</summary>
    private static async Task CreateBenefitTablesAsync(SqliteConnection connection)
    {
        const string ddl = @"
CREATE TABLE customer_benefit_budgets (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  ParticipantPlanSubscriptionId INTEGER NOT NULL, CustomerPlanId INTEGER NOT NULL,
  PeriodStart TEXT NOT NULL, PeriodEnd TEXT NOT NULL,
  FundedAmount TEXT NOT NULL, ReservedAmount TEXT NOT NULL, ConsumedAmount TEXT NOT NULL,
  CurrencyCode TEXT NOT NULL, Status INTEGER NOT NULL, Version INTEGER NOT NULL,
  PublicId TEXT NULL, ModifyHost TEXT NULL, ModifyUserId INTEGER NULL, ModifyDate TEXT NULL,
  CreateUserId INTEGER NULL, CreateHost TEXT NULL, CreateDate TEXT NULL,
  IsDeleted INTEGER NOT NULL, DeletedAt TEXT NULL, DeletedBy INTEGER NULL, IsActive INTEGER NOT NULL);
CREATE TABLE customer_benefit_reservations (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  BudgetId INTEGER NOT NULL, Amount TEXT NOT NULL, Status INTEGER NOT NULL, ContextRef TEXT NOT NULL,
  ReservedAtUtc TEXT NOT NULL, ConsumedAtUtc TEXT NULL, ReleasedAtUtc TEXT NULL,
  PublicId TEXT NULL, ModifyHost TEXT NULL, ModifyUserId INTEGER NULL, ModifyDate TEXT NULL,
  CreateUserId INTEGER NULL, CreateHost TEXT NULL, CreateDate TEXT NULL,
  IsDeleted INTEGER NOT NULL, DeletedAt TEXT NULL, DeletedBy INTEGER NULL, IsActive INTEGER NOT NULL);
CREATE UNIQUE INDEX ux_res_ctx ON customer_benefit_reservations (BudgetId, ContextRef);";
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = ddl;
        await cmd.ExecuteNonQueryAsync();
    }
}
