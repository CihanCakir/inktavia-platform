using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.ResolveMonthlySellThroughSettlement;
using Aizen.Modules.CargoDry.Application.Services;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Aizen.Modules.CargoDry.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.CargoDry.Repository.UnitTests;

/// <summary>
/// Coverage for the sell-through settlement link fix: link-at-activation, the second-pass linker (idempotent),
/// ResolveMonthly correctness + the wipe guard (Task 3), and the damaged-settlement repair path (Task 2).
/// </summary>
public sealed class CargoDrySettlementLinkingTests
{
    private const long   ProviderId = 10;
    private const string Currency   = "TRY";
    private const string Product    = "STANDARD-90";
    private const long   AgreementId = 5;
    private static readonly DateTime Sept2026 = new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
    private const int TargetYm = 202609;

    private static CargoDryDbContext NewDb()
        => new(new DbContextOptionsBuilder<CargoDryDbContext>()
            .UseInMemoryDatabase($"cargodry-link-{Guid.NewGuid():N}").Options);

    private sealed class NoopMilestoneEvaluator : ICargoDryProviderMilestoneEvaluator
    {
        public Task EvaluateAfterSaleAsync(long providerProfileId, DateTimeOffset occurredAtUtc, CancellationToken ct)
            => Task.CompletedTask;
    }

    private static CargoDrySalesAttributionEntity NewConsignmentAttribution(
        long kitId,
        DateTime createdAt,
        CargoDrySalesAttributionStatus status = CargoDrySalesAttributionStatus.Attributed,
        bool resolved = false)
    {
        var a = CargoDrySalesAttributionEntity.Create(
            kitId:                  kitId,
            serialNumber:           $"SN-{kitId}",
            kitCode:                $"KIT-{kitId}",
            productCode:            Product,
            batchCode:              "B1",
            salesChannel:           SalesChannel.ConsignmentSellThrough,
            commercialModel:        CargoDryCommercialModel.PrincipalSale,
            initialStatus:          status,
            nowUtc:                 createdAt,
            providerProfileId:      ProviderId,
            consignmentAgreementId: AgreementId,
            currencyCode:           Currency);
        if (resolved)
            a.ResolveFinancials(149.99m, 0.20m, Currency, createdAt, resolvedByUserId: 1);
        return a;
    }

    private static CargoDrySellThroughSettlementEntity NewSettlement()
        => CargoDrySellThroughSettlementEntity.Create(
            settlementCode:         "STS-10-TRY-STANDARD-90-202609",
            consignmentAgreementId: AgreementId,
            providerProfileId:      ProviderId,
            productCode:            Product,
            batchCode:              null,
            currencyCode:           Currency,
            periodStartUtc:         new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            periodEndUtc:           new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            nowUtc:                 Sept2026);

    // ── Task 4: rounding ────────────────────────────────────────────────────────

    [Fact]
    public void ResolveFinancials_rounds_provider_share_to_two_decimals()
    {
        var a = NewConsignmentAttribution(1, Sept2026);
        a.ResolveFinancials(149.99m, 0.20m, Currency, Sept2026, resolvedByUserId: 1);

        a.ProviderShareAmount.Should().Be(30.00m); // 149.99 × 0.20 = 29.998 → 30.00 (was 29.998 at 4dp)
        a.PlatformShareAmount.Should().Be(119.99m);
    }

    // ── Task 1: entity link semantics ─────────────────────────────────────────────

    [Fact]
    public void LinkToSettlement_sets_fk_and_settlement_pending_and_is_set_once()
    {
        var a = NewConsignmentAttribution(1, Sept2026); // Attributed (orphan-heal shape)
        a.LinkToSettlement(99, Sept2026);

        a.SellThroughSettlementId.Should().Be(99);
        a.Status.Should().Be(CargoDrySalesAttributionStatus.SettlementPending);

        a.Invoking(x => x.LinkToSettlement(100, Sept2026))
            .Should().Throw<InvalidOperationException>("the FK is set-once");
    }

    [Fact]
    public void LinkToSettlement_from_already_settlement_pending_is_not_a_double_transition()
    {
        var a = NewConsignmentAttribution(1, Sept2026, status: CargoDrySalesAttributionStatus.SettlementPending);
        a.LinkToSettlement(99, Sept2026); // activation shape: already SettlementPending
        a.Status.Should().Be(CargoDrySalesAttributionStatus.SettlementPending);
        a.SellThroughSettlementId.Should().Be(99);
    }

    [Fact]
    public void LinkToSettlement_refuses_terminal_status()
    {
        var settled = NewConsignmentAttribution(1, Sept2026, status: CargoDrySalesAttributionStatus.SettlementPending);
        settled.LinkToSettlement(1, Sept2026);
        settled.MarkSettled();
        settled.Invoking(x => x.LinkToSettlement(2, Sept2026)).Should().Throw<InvalidOperationException>();

        var cancelled = NewConsignmentAttribution(2, Sept2026);
        cancelled.Cancel();
        cancelled.Invoking(x => x.LinkToSettlement(3, Sept2026)).Should().Throw<InvalidOperationException>();
    }

    // ── Task 2: settlement reopen guard ───────────────────────────────────────────

    [Fact]
    public void ReopenForResolution_flips_ready_to_pending_but_refuses_once_paid()
    {
        var s = NewSettlement();
        s.MarkReadyForSettlement(Sept2026);
        s.ReopenForResolution();
        s.Status.Should().Be(CargoDrySellThroughSettlementStatus.Pending);
        s.ReadyForSettlementAtUtc.Should().BeNull();

        // With a prepared payment it must refuse.
        s.MarkReadyForSettlement(Sept2026);
        s.MarkPaymentPrepared(payoutRecordId: 77, preparedByUserId: 1, preparedAtUtc: Sept2026);
        // now Scheduled → reopen only accepts ReadyForSettlement
        s.Invoking(x => x.ReopenForResolution()).Should().Throw<InvalidOperationException>();
    }

    // ── Repository query methods ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUnlinked_returns_only_unlinked_consignment_in_period_and_not_terminal()
    {
        await using var db = NewDb();
        var repo = new CargoDrySalesAttributionRepository(db);

        var unlinked   = NewConsignmentAttribution(1, Sept2026);                          // eligible
        var linked     = NewConsignmentAttribution(2, Sept2026); linked.LinkToSettlement(1, Sept2026); // excluded: linked
        var cancelled  = NewConsignmentAttribution(3, Sept2026); cancelled.Cancel();      // excluded: terminal
        var outOfMonth = NewConsignmentAttribution(4, new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc)); // excluded: period
        var direct     = CargoDrySalesAttributionEntity.Create(
            5, "SN-5", "KIT-5", Product, "B1", SalesChannel.DirectSale, CargoDryCommercialModel.PrincipalSale,
            CargoDrySalesAttributionStatus.Attributed, Sept2026);                          // excluded: channel
        db.SalesAttributions.AddRange(unlinked, linked, cancelled, outOfMonth, direct);
        await db.SaveChangesAsync();

        var res = await repo.GetUnlinkedConsignmentSellThroughForPeriodAsync(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), default);

        res.Select(x => x.KitId).Should().Equal(1);
    }

    [Fact]
    public async Task GetByProviderCurrencyProductPeriod_finds_ready_for_settlement_not_only_pending()
    {
        await using var db = NewDb();
        var repo = new CargoDrySellThroughSettlementRepository(db);
        var s = NewSettlement();
        s.MarkReadyForSettlement(Sept2026); // NOT pending — GetOpen... would miss it
        db.SellThroughSettlements.Add(s);
        await db.SaveChangesAsync();

        var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        (await repo.GetOpenForProviderCurrencyProductPeriodAsync(ProviderId, Currency, Product, start, end, default))
            .Should().BeNull("the Pending-only lookup must not see a ReadyForSettlement settlement");
        (await repo.GetByProviderCurrencyProductPeriodAsync(ProviderId, Currency, Product, start, end, default))
            .Should().NotBeNull("the repair lookup finds it regardless of status");
    }

    // ── Task 1b: second-pass linker ───────────────────────────────────────────────

    [Fact]
    public async Task SecondPass_links_orphans_to_existing_settlement_and_is_idempotent()
    {
        await using var db = NewDb();
        var attrRepo = new CargoDrySalesAttributionRepository(db);
        var setlRepo = new CargoDrySellThroughSettlementRepository(db);

        var settlement = NewSettlement(); // Pending
        db.SellThroughSettlements.Add(settlement);
        db.SalesAttributions.AddRange(
            NewConsignmentAttribution(1, Sept2026, resolved: true),
            NewConsignmentAttribution(2, Sept2026, resolved: true));
        await db.SaveChangesAsync();

        var linker = new CargoDrySettlementLinkingService(attrRepo, setlRepo, NullLogger<CargoDrySettlementLinkingService>.Instance);

        var linked = await linker.LinkUnlinkedForPeriodAsync(TargetYm, triggeredByUserId: 1, default);
        linked.Should().Be(2);

        var afterLink = await attrRepo.GetBySettlementIdAsync(settlement.Id, default);
        afterLink.Should().HaveCount(2);
        afterLink.Should().OnlyContain(a => a.Status == CargoDrySalesAttributionStatus.SettlementPending);

        // Idempotent — second run links nothing.
        (await linker.LinkUnlinkedForPeriodAsync(TargetYm, 1, default)).Should().Be(0);
    }

    [Fact]
    public async Task SecondPass_creates_the_period_settlement_when_missing()
    {
        await using var db = NewDb();
        var attrRepo = new CargoDrySalesAttributionRepository(db);
        var setlRepo = new CargoDrySellThroughSettlementRepository(db);

        db.SalesAttributions.Add(NewConsignmentAttribution(1, Sept2026, resolved: true));
        await db.SaveChangesAsync();

        var linker = new CargoDrySettlementLinkingService(attrRepo, setlRepo, NullLogger<CargoDrySettlementLinkingService>.Instance);
        (await linker.LinkUnlinkedForPeriodAsync(TargetYm, 1, default)).Should().Be(1);

        var created = await setlRepo.GetByProviderCurrencyProductPeriodAsync(
            ProviderId, Currency, Product,
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), default);
        created.Should().NotBeNull();
        var links = await attrRepo.GetBySettlementIdAsync(created!.Id, default);
        links.Should().HaveCount(1);
    }

    // ── Task 3 + resolve correctness ──────────────────────────────────────────────

    private static ResolveMonthlySellThroughSettlementCommandHandler NewResolveHandler(CargoDryDbContext db)
        => new(new CargoDrySellThroughSettlementRepository(db), new CargoDrySalesAttributionRepository(db));

    [Fact]
    public async Task ResolveMonthly_with_linked_attributions_sums_correctly()
    {
        await using var db = NewDb();
        var settlement = NewSettlement();
        db.SellThroughSettlements.Add(settlement);
        await db.SaveChangesAsync();

        foreach (var kitId in new long[] { 1, 2 })
        {
            var a = NewConsignmentAttribution(kitId, Sept2026, resolved: true);
            a.LinkToSettlement(settlement.Id, Sept2026);
            db.SalesAttributions.Add(a);
        }
        await db.SaveChangesAsync();

        await NewResolveHandler(db).Handle(new ResolveMonthlySellThroughSettlementCommand
        {
            SettlementId = settlement.Id, ResolvedByUserId = 1, ResolutionNote = "test",
        }, default);

        var reloaded = await new CargoDrySellThroughSettlementRepository(db).GetByIdAsync(settlement.Id, default);
        reloaded!.Status.Should().Be(CargoDrySellThroughSettlementStatus.ReadyForSettlement);
        reloaded.TotalKitCount.Should().Be(2);
        reloaded.TotalSaleAmount.Should().Be(299.98m);          // 149.99 × 2
        reloaded.ProviderPayoutAmount.Should().Be(60.00m);      // 30.00 × 2
    }

    [Fact]
    public async Task ResolveMonthly_refuses_to_wipe_when_zero_linked_but_kits_recorded()
    {
        await using var db = NewDb();
        var settlement = NewSettlement();
        settlement.AddAttribution(0m, 0m, Sept2026); // activation-time counts, FK never set (the dead-link bug)
        settlement.AddAttribution(0m, 0m, Sept2026);
        db.SellThroughSettlements.Add(settlement);
        await db.SaveChangesAsync();

        await NewResolveHandler(db).Invoking(h => h.Handle(new ResolveMonthlySellThroughSettlementCommand
            {
                SettlementId = settlement.Id, ResolvedByUserId = 1, ResolutionNote = null,
            }, default))
            .Should().ThrowAsync<AizenBusinessException>()
            .WithMessage("*0 linked attributions*");

        var reloaded = await new CargoDrySellThroughSettlementRepository(db).GetByIdAsync(settlement.Id, default);
        reloaded!.TotalKitCount.Should().Be(2, "totals must NOT be wiped");
        reloaded.Status.Should().Be(CargoDrySellThroughSettlementStatus.Pending);
    }

    // ── Task 2: repair a damaged (ReadyForSettlement, zeroed) settlement ───────────

    [Fact]
    public async Task Damaged_ready_settlement_recovers_after_linking_via_reresolve()
    {
        await using var db = NewDb();

        // Reproduce the historical damage: resolved while 0 attributions were linked → totals wiped, marked Ready.
        var settlement = NewSettlement();
        settlement.RecalculateTotals(0, 0m, 0m);
        settlement.MarkReadyForSettlement(Sept2026);
        db.SellThroughSettlements.Add(settlement);

        // The orphans that should have been in it (resolved), still unlinked.
        db.SalesAttributions.AddRange(
            NewConsignmentAttribution(1, Sept2026, resolved: true),
            NewConsignmentAttribution(2, Sept2026, resolved: true));
        await db.SaveChangesAsync();

        // Heal: second-pass linker attaches the orphans to the existing (Ready) settlement.
        var attrRepo = new CargoDrySalesAttributionRepository(db);
        var setlRepo = new CargoDrySellThroughSettlementRepository(db);
        var linker = new CargoDrySettlementLinkingService(attrRepo, setlRepo, NullLogger<CargoDrySettlementLinkingService>.Instance);
        (await linker.LinkUnlinkedForPeriodAsync(TargetYm, 1, default)).Should().Be(2);

        // Repair: re-resolve is now permitted from ReadyForSettlement (no payout/invoice) → recomputes correct totals.
        await NewResolveHandler(db).Handle(new ResolveMonthlySellThroughSettlementCommand
        {
            SettlementId = settlement.Id, ResolvedByUserId = 1, ResolutionNote = "repair",
        }, default);

        var reloaded = await setlRepo.GetByIdAsync(settlement.Id, default);
        reloaded!.Status.Should().Be(CargoDrySellThroughSettlementStatus.ReadyForSettlement);
        reloaded.TotalKitCount.Should().Be(2);
        reloaded.ProviderPayoutAmount.Should().Be(60.00m);
    }

    // ── Task 1a: link-at-activation through the real activation service ───────────

    [Fact]
    public async Task Activation_links_the_attribution_to_the_settlement()
    {
        await using var db = NewDb();

        var agreement = CargoDryConsignmentAgreementEntity.Create(
            agreementCode: "AGR-1", providerProfileId: ProviderId, productCode: Product,
            consignmentRate: 0.20m, minimumSettlementAmount: 0m, currencyCode: Currency,
            maxKitCount: 100, startDateUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        agreement.Activate();
        db.ConsignmentAgreements.Add(agreement);

        var kit = CargoDryKitEntity.Create("SN-ACT", "KIT-ACT", "qr", Product, "B1");
        kit.AssignToProvider(ProviderId, SalesChannel.ConsignmentSellThrough, CargoDryCommercialModel.PrincipalSale);
        db.Kits.Add(kit);
        await db.SaveChangesAsync();

        var svc = new CargoDryCommercialActivationService(
            new CargoDryKitRepository(db),
            new CargoDryConsignmentAgreementRepository(db),
            new CargoDryProviderInventoryRepository(db),
            new CargoDryInventoryMovementRepository(db),
            new CargoDrySalesAttributionRepository(db),
            new CargoDrySellThroughSettlementRepository(db),
            new NoopMilestoneEvaluator(),
            NullLogger<CargoDryCommercialActivationService>.Instance);

        await svc.ResolveAsync(kit.Id, activatedByUserId: 1, default);
        await new CargoDrySalesAttributionRepository(db).SaveChangesAsync(default);

        var attribution = await new CargoDrySalesAttributionRepository(db).GetByKitIdAsync(kit.Id, default);
        attribution.Should().NotBeNull();
        attribution!.SellThroughSettlementId.Should().NotBeNull("link-at-activation must stamp the FK");
        attribution.Status.Should().Be(CargoDrySalesAttributionStatus.SettlementPending);

        var settlement = await new CargoDrySellThroughSettlementRepository(db)
            .GetByIdAsync(attribution.SellThroughSettlementId!.Value, default);
        settlement.Should().NotBeNull();
        settlement!.ProviderProfileId.Should().Be(ProviderId);
    }
}
