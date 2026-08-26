using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionsPaged;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySellThroughSettlementsPaged;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aizen.Modules.CargoDry.Repository.UnitTests;

/// <summary>
/// Root-cause regression tests running against a REAL, freshly-migrated PostgreSQL schema
/// (see <see cref="MigratedPostgresFixture"/>). The two admin endpoints returned HTTP 500 with
/// <c>Npgsql.PostgresException: 42703: column "..." does not exist</c> because the migrated
/// schema was missing columns the entity model reads (sales_attributions.TierAtSale /
/// TierBonusRate, and — on drifted databases — sell_through_settlements.InvoiceId and its
/// siblings). An InMemory/EnsureCreated schema is built from the model and therefore hides this
/// class of bug entirely; only applying the actual migrations reproduces it.
///
/// Skips gracefully when the compose PostgreSQL is not reachable.
/// </summary>
[Collection("migrated-postgres")]
public sealed class CommercialPagedMigratedSchemaTests
{
    private static readonly DateTime NowUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly MigratedPostgresFixture _fx;

    public CommercialPagedMigratedSchemaTests(MigratedPostgresFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task SalesAttributions_EmptyTable_On_Migrated_Schema_ReturnsEmptyPage()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);

        await using var db = _fx.NewDbContext();
        var handler = new GetCargoDrySalesAttributionsPagedQueryHandler(
            new CargoDrySalesAttributionRepository(db));

        var res = await handler.Handle(new GetCargoDrySalesAttributionsPagedQuery(), CancellationToken.None);

        res.PagedResult.Items.Should().BeEmpty();
        res.PagedResult.Total.Should().Be(0);
    }

    [SkippableFact]
    public async Task Settlements_EmptyTable_On_Migrated_Schema_ReturnsEmptyPage()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);

        await using var db = _fx.NewDbContext();
        var handler = new GetCargoDrySellThroughSettlementsPagedQueryHandler(
            new CargoDrySellThroughSettlementRepository(db));

        var res = await handler.Handle(new GetCargoDrySellThroughSettlementsPagedQuery(), CancellationToken.None);

        res.PagedResult.Items.Should().BeEmpty();
        res.PagedResult.Total.Should().Be(0);
    }

    [SkippableFact]
    public async Task SalesAttributions_NullFinancialRow_On_Migrated_Schema_MapsWithoutThrowing()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);

        await using (var seed = _fx.NewDbContext())
        {
            var row = CargoDrySalesAttributionEntity.Create(
                kitId: 1, serialNumber: "SN-1", kitCode: "KIT-1", productCode: "PROD-1",
                batchCode: null,
                salesChannel: SalesChannel.ConsignmentSellThrough,
                commercialModel: CargoDryCommercialModel.MarketplaceCommission,
                initialStatus: CargoDrySalesAttributionStatus.SettlementPending,
                nowUtc: NowUtc);
            seed.SalesAttributions.Add(row);
            await seed.SaveChangesAsync();
        }

        await using var db = _fx.NewDbContext();
        var handler = new GetCargoDrySalesAttributionsPagedQueryHandler(
            new CargoDrySalesAttributionRepository(db));

        var res = await handler.Handle(new GetCargoDrySalesAttributionsPagedQuery(), CancellationToken.None);

        res.PagedResult.Items.Should().HaveCount(1);
        res.PagedResult.Items[0].SalePrice.Should().BeNull();
        res.PagedResult.Items[0].CurrencyCode.Should().BeNull();
    }

    /// <summary>
    /// Reproduces the exact production failure and proves the reconciliation repairs it:
    /// simulate the drifted database by dropping the Phase-4C invoice columns, confirm the
    /// settlements handler throws <c>PostgresException 42703 (column "InvoiceId" does not exist)</c>,
    /// then re-run the reconciliation's idempotent SQL and confirm the endpoint returns an empty
    /// page again. Mirrors ReconcileCargoDryCommercialSchemaDrift.Up().
    /// </summary>
    [SkippableFact]
    public async Task Settlements_DroppedInvoiceColumns_Throw_ThenReconciliationRepairs()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);

        // ── Simulate drift: strip the columns the entity model reads ──────────────────
        await using (var drift = _fx.NewDbContext())
        {
            await drift.Database.ExecuteSqlRawAsync(
                "ALTER TABLE cargodry.sell_through_settlements DROP COLUMN IF EXISTS \"InvoiceId\";" +
                "ALTER TABLE cargodry.sell_through_settlements DROP COLUMN IF EXISTS \"InvoicePreparationNote\";" +
                "ALTER TABLE cargodry.sell_through_settlements DROP COLUMN IF EXISTS \"InvoicePreparedAtUtc\";" +
                "ALTER TABLE cargodry.sell_through_settlements DROP COLUMN IF EXISTS \"InvoicePreparedByUserId\";");
        }

        await using (var broken = _fx.NewDbContext())
        {
            var handler = new GetCargoDrySellThroughSettlementsPagedQueryHandler(
                new CargoDrySellThroughSettlementRepository(broken));

            var act = async () =>
                await handler.Handle(new GetCargoDrySellThroughSettlementsPagedQuery(), CancellationToken.None);

            (await act.Should().ThrowAsync<PostgresException>())
                .Which.SqlState.Should().Be(PostgresErrorCodes.UndefinedColumn); // 42703
        }

        // ── Reconcile: idempotent ADD COLUMN IF NOT EXISTS (== migration Up) ──────────
        await using (var repair = _fx.NewDbContext())
        {
            await repair.Database.ExecuteSqlRawAsync(
                "ALTER TABLE cargodry.sell_through_settlements ADD COLUMN IF NOT EXISTS \"InvoiceId\" bigint NULL;" +
                "ALTER TABLE cargodry.sell_through_settlements ADD COLUMN IF NOT EXISTS \"InvoicePreparationNote\" character varying(1000) NULL;" +
                "ALTER TABLE cargodry.sell_through_settlements ADD COLUMN IF NOT EXISTS \"InvoicePreparedAtUtc\" timestamp with time zone NULL;" +
                "ALTER TABLE cargodry.sell_through_settlements ADD COLUMN IF NOT EXISTS \"InvoicePreparedByUserId\" bigint NULL;");
        }

        await using (var fixed_ = _fx.NewDbContext())
        {
            var handler = new GetCargoDrySellThroughSettlementsPagedQueryHandler(
                new CargoDrySellThroughSettlementRepository(fixed_));

            var res = await handler.Handle(new GetCargoDrySellThroughSettlementsPagedQuery(), CancellationToken.None);

            res.PagedResult.Items.Should().BeEmpty();
            res.PagedResult.Total.Should().Be(0);
        }
    }
}
