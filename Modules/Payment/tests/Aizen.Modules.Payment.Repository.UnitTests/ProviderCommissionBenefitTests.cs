using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using Aizen.Modules.Payment.Repository.Seed;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

public sealed class ProviderCommissionBenefitTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PaymentDbContext NewInMemoryDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"pcb-{Guid.NewGuid():N}").Options);

    // ── Seed: disabled example rule (benefits OFF by default) ───────────────────

    [Fact]
    public async Task Seed_Adds_Disabled_Example_Rule_Not_A_Candidate()
    {
        await using var db = NewInMemoryDb();
        await new ProviderCommissionBenefitSeed(db, NullLogger<ProviderCommissionBenefitSeed>.Instance).SeedAsync();

        (await db.ProviderCommissionBenefitRules.CountAsync()).Should().Be(1);
        var repo = new ProviderCommissionBenefitRuleRepository(db);
        (await repo.GetActiveCandidatesAsync("TRY", DateTime.UtcNow))
            .Should().BeEmpty("the seeded example rule is disabled (Inactive) so it is never a resolver candidate");
    }

    [Fact]
    public async Task Seed_Is_Idempotent()
    {
        await using var db = NewInMemoryDb();
        var seed = new ProviderCommissionBenefitSeed(db, NullLogger<ProviderCommissionBenefitSeed>.Instance);
        await seed.SeedAsync();
        await seed.SeedAsync();
        (await db.ProviderCommissionBenefitRules.CountAsync()).Should().Be(1);
    }

    // ── Entitlement service: reserve / consume / release / exhausted / idempotent ─

    private static async Task<(PaymentDbContext db, long entId)> SeedEntitlement(long? usageLimit, decimal? maxGmv)
    {
        var db = NewInMemoryDb();
        var e = ProviderCommissionBenefitEntitlementEntity.Grant("PCE-1", 7, 1, From, null, usageLimit, maxGmv);
        db.ProviderCommissionBenefitEntitlements.Add(e);
        await db.SaveChangesAsync();
        return (db, e.Id);
    }

    [Fact]
    public async Task Reserve_Then_Consume_Tracks_Counters()
    {
        var (db, entId) = await SeedEntitlement(usageLimit: 3, maxGmv: 1000m);
        await using var _ = db;
        var svc = new CommissionBenefitEntitlementService(new ProviderCommissionBenefitEntitlementRepository(db));

        var usage = await svc.ReserveAsync(entId, 400m, 4m, "offer-1");
        (await db.ProviderCommissionBenefitEntitlements.AsNoTracking().FirstAsync(x => x.Id == entId)).ReservedGMV.Should().Be(400m);

        await svc.ConsumeAsync(usage.Id);
        var after = await db.ProviderCommissionBenefitEntitlements.AsNoTracking().FirstAsync(x => x.Id == entId);
        after.ConsumedGMV.Should().Be(400m);
        after.UsedCount.Should().Be(1);
    }

    [Fact]
    public async Task Reserve_Over_Limit_Is_Exhausted()
    {
        var (db, entId) = await SeedEntitlement(usageLimit: 1, maxGmv: null);
        await using var _ = db;
        var svc = new CommissionBenefitEntitlementService(new ProviderCommissionBenefitEntitlementRepository(db));
        await svc.ReserveAsync(entId, 10m, 1m, "offer-1");

        Func<Task> act = () => svc.ReserveAsync(entId, 10m, 1m, "offer-2");
        await act.Should().ThrowAsync<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitExhausted);
    }

    [Fact]
    public async Task Duplicate_Consume_Is_Idempotent()
    {
        var (db, entId) = await SeedEntitlement(usageLimit: 3, maxGmv: 1000m);
        await using var _ = db;
        var svc = new CommissionBenefitEntitlementService(new ProviderCommissionBenefitEntitlementRepository(db));

        var usage = await svc.ReserveAsync(entId, 400m, 4m, "offer-1");
        await svc.ConsumeAsync(usage.Id);
        await svc.ConsumeAsync(usage.Id);   // duplicate webhook

        (await db.ProviderCommissionBenefitEntitlements.AsNoTracking().FirstAsync(x => x.Id == entId)).UsedCount.Should().Be(1);
    }

    [Fact]
    public async Task Reserve_Is_Idempotent_Per_ContextRef()
    {
        var (db, entId) = await SeedEntitlement(usageLimit: 3, maxGmv: 1000m);
        await using var _ = db;
        var svc = new CommissionBenefitEntitlementService(new ProviderCommissionBenefitEntitlementRepository(db));

        var u1 = await svc.ReserveAsync(entId, 100m, 1m, "offer-1");
        var u2 = await svc.ReserveAsync(entId, 100m, 1m, "offer-1");
        u2.Id.Should().Be(u1.Id);
        (await db.ProviderCommissionBenefitEntitlements.AsNoTracking().FirstAsync(x => x.Id == entId)).ReservedCount.Should().Be(1);
    }

    // ── Concurrent reserve on the same entitlement → one wins (SQLite) ───────────

    [Fact]
    public async Task Concurrent_Reserve_On_Same_Entitlement_One_Wins()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<PaymentDbContext>().UseSqlite(connection).Options;
            await CreateEntitlementTablesAsync(connection);

            long entId;
            await using (var setup = new PaymentDbContext(options))
            {
                var e = ProviderCommissionBenefitEntitlementEntity.Grant("PCE-1", 7, 1, From, null, 1, 1000m);
                setup.ProviderCommissionBenefitEntitlements.Add(e);
                await setup.SaveChangesAsync();
                entId = e.Id;
            }

            await using var dbA = new PaymentDbContext(options);
            await using var dbB = new PaymentDbContext(options);
            var repoA = new ProviderCommissionBenefitEntitlementRepository(dbA);
            var repoB = new ProviderCommissionBenefitEntitlementRepository(dbB);

            var entA = await repoA.GetByIdAsync(entId);   // Version 0
            var entB = await repoB.GetByIdAsync(entId);   // Version 0

            await repoA.AddUsageAsync(entA!.Reserve(100m, 1m, "offer-A", DateTime.UtcNow));
            await repoA.SaveChangesConcurrencySafeAsync();   // first wins → Version 1

            await repoB.AddUsageAsync(entB!.Reserve(100m, 1m, "offer-B", DateTime.UtcNow));
            Func<Task> second = () => repoB.SaveChangesConcurrencySafeAsync();
            await second.Should().ThrowAsync<AizenBusinessException>()
               .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitConcurrencyConflict);

            await using var verify = new PaymentDbContext(options);
            (await verify.ProviderCommissionBenefitEntitlements.AsNoTracking().FirstAsync(x => x.Id == entId)).ReservedCount.Should().Be(1);
        }
        finally
        {
            connection.Close();
        }
    }

    private static async Task CreateEntitlementTablesAsync(SqliteConnection connection)
    {
        const string ddl = @"
CREATE TABLE provider_commission_benefit_entitlements (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  EntitlementCode TEXT NULL, ProviderProfileId INTEGER NOT NULL, BenefitRuleId INTEGER NOT NULL,
  GrantedFrom TEXT NOT NULL, GrantedTo TEXT NULL, UsageLimit INTEGER NULL, UsedCount INTEGER NOT NULL,
  ReservedCount INTEGER NOT NULL, MaximumEligibleGMV TEXT NULL, ConsumedGMV TEXT NOT NULL, ReservedGMV TEXT NOT NULL,
  Status INTEGER NOT NULL, Version INTEGER NOT NULL,
  PublicId TEXT NULL, ModifyHost TEXT NULL, ModifyUserId INTEGER NULL, ModifyDate TEXT NULL,
  CreateUserId INTEGER NULL, CreateHost TEXT NULL, CreateDate TEXT NULL,
  IsDeleted INTEGER NOT NULL, DeletedAt TEXT NULL, DeletedBy INTEGER NULL, IsActive INTEGER NOT NULL);
CREATE TABLE provider_commission_benefit_usages (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  EntitlementId INTEGER NOT NULL, ContextRef TEXT NOT NULL, GmvAmount TEXT NOT NULL, BenefitAmount TEXT NOT NULL,
  Status INTEGER NOT NULL, ReservedAtUtc TEXT NOT NULL, ConsumedAtUtc TEXT NULL, ReleasedAtUtc TEXT NULL,
  PublicId TEXT NULL, ModifyHost TEXT NULL, ModifyUserId INTEGER NULL, ModifyDate TEXT NULL,
  CreateUserId INTEGER NULL, CreateHost TEXT NULL, CreateDate TEXT NULL,
  IsDeleted INTEGER NOT NULL, DeletedAt TEXT NULL, DeletedBy INTEGER NULL, IsActive INTEGER NOT NULL);
CREATE UNIQUE INDEX ux_usage_ctx ON provider_commission_benefit_usages (EntitlementId, ContextRef);";
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = ddl;
        await cmd.ExecuteNonQueryAsync();
    }
}
