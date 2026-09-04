using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionsPaged;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySellThroughSettlementsPaged;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Aizen.Modules.CargoDry.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.UnitTests;

/// <summary>
/// Handler-contract regression tests for the two admin commercial paged endpoints that returned
/// HTTP 500. These exercise the query handler + repository + DTO mapping over an InMemory store,
/// pinning the behaviour the endpoints must honour: an empty table yields a well-formed empty
/// page (not an error), and rows whose optional financial fields are null map without throwing.
/// </summary>
public sealed class CommercialPagedHandlerLogicTests
{
    private static readonly DateTime NowUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CargoDryDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<CargoDryDbContext>()
            .UseInMemoryDatabase($"cargodry-{Guid.NewGuid():N}")
            .Options;
        return new CargoDryDbContext(options);
    }

    // ── Sales attributions ───────────────────────────────────────────────────────

    [Fact]
    public async Task SalesAttributions_EmptyTable_ReturnsEmptyPage()
    {
        await using var db = NewDb();
        var handler = new GetCargoDrySalesAttributionsPagedQueryHandler(
            new CargoDrySalesAttributionRepository(db));

        var res = await handler.Handle(new GetCargoDrySalesAttributionsPagedQuery(), CancellationToken.None);

        res.PagedResult.Should().NotBeNull();
        res.PagedResult.Items.Should().BeEmpty();
        res.PagedResult.Total.Should().Be(0);
        res.PagedResult.Page.Should().Be(1);
        res.PagedResult.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task SalesAttributions_RowWithNullFinancials_MapsWithoutThrowing()
    {
        await using var db = NewDb();
        // Attribution created before financial resolution: SalePrice / CommissionAmount /
        // ProviderShareAmount / CurrencyCode all null — the exact shape that must not crash the mapper.
        var row = CargoDrySalesAttributionEntity.Create(
            kitId: 1,
            serialNumber: "SN-1",
            kitCode: "KIT-1",
            productCode: "PROD-1",
            batchCode: null,
            salesChannel: SalesChannel.ConsignmentSellThrough,
            commercialModel: CargoDryCommercialModel.MarketplaceCommission,
            initialStatus: CargoDrySalesAttributionStatus.SettlementPending,
            nowUtc: NowUtc);
        db.SalesAttributions.Add(row);
        await db.SaveChangesAsync();

        var handler = new GetCargoDrySalesAttributionsPagedQueryHandler(
            new CargoDrySalesAttributionRepository(db));

        var res = await handler.Handle(new GetCargoDrySalesAttributionsPagedQuery(), CancellationToken.None);

        res.PagedResult.Items.Should().HaveCount(1);
        var item = res.PagedResult.Items[0];
        item.SalePrice.Should().BeNull();
        item.CommissionAmount.Should().BeNull();
        item.ProviderShareAmount.Should().BeNull();
        item.CurrencyCode.Should().BeNull();
        item.IsFinanciallyResolved.Should().BeFalse();
        item.StatusName.Should().Be(CargoDrySalesAttributionStatus.SettlementPending.ToString());
    }

    // ── Sell-through settlements ──────────────────────────────────────────────────

    [Fact]
    public async Task Settlements_EmptyTable_ReturnsEmptyPage()
    {
        await using var db = NewDb();
        var handler = new GetCargoDrySellThroughSettlementsPagedQueryHandler(
            new CargoDrySellThroughSettlementRepository(db));

        var res = await handler.Handle(new GetCargoDrySellThroughSettlementsPagedQuery(), CancellationToken.None);

        res.PagedResult.Should().NotBeNull();
        res.PagedResult.Items.Should().BeEmpty();
        res.PagedResult.Total.Should().Be(0);
        res.PagedResult.Page.Should().Be(1);
        res.PagedResult.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task Settlements_RowWithNullOptionalFinancials_MapsWithoutThrowing()
    {
        await using var db = NewDb();
        // A freshly-created settlement leaves every Phase-4 optional field (InvoiceId,
        // PayoutRecordId, PayoutCompletionReference, …) null — must map cleanly.
        var row = CargoDrySellThroughSettlementEntity.Create(
            settlementCode: "STS-2026-08-001",
            consignmentAgreementId: 10,
            providerProfileId: 20,
            productCode: "PROD-1",
            batchCode: null,
            currencyCode: "TRY",
            periodStartUtc: NowUtc,
            periodEndUtc: NowUtc.AddMonths(1),
            nowUtc: NowUtc);
        db.SellThroughSettlements.Add(row);
        await db.SaveChangesAsync();

        var handler = new GetCargoDrySellThroughSettlementsPagedQueryHandler(
            new CargoDrySellThroughSettlementRepository(db));

        var res = await handler.Handle(new GetCargoDrySellThroughSettlementsPagedQuery(), CancellationToken.None);

        res.PagedResult.Items.Should().HaveCount(1);
        var item = res.PagedResult.Items[0];
        item.InvoiceId.Should().BeNull();
        item.PayoutRecordId.Should().BeNull();
        item.PayoutCompletionReference.Should().BeNull();
        item.SettlementCode.Should().Be("STS-2026-08-001");
    }
}
