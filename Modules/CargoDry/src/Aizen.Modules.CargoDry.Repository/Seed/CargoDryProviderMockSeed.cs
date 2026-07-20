using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Repository.Seed;

/// <summary>
/// Seeds CargoDry kits + inventory + movements for provider2 (100011).
/// Dev/local only, each block independently guarded (completes partial prior seeds).
/// </summary>
public sealed class CargoDryProviderMockSeed
{
    private const long Provider2 = 100011;
    private const string BatchCode = "202507-CONS-PRV2";

    private readonly CargoDryDbContext _db;
    private readonly ILogger<CargoDryProviderMockSeed> _logger;

    public CargoDryProviderMockSeed(CargoDryDbContext db, ILogger<CargoDryProviderMockSeed> logger)
    { _db = db; _logger = logger; }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        try
        {
            var nowUtc = DateTime.UtcNow;

            // ── Kits (skip if already seeded) ────────────────────────────────
            if (!await _db.Kits.AnyAsync(k => k.ProviderProfileId == Provider2, ct))
            {
                var batch = CargoDryBatchEntity.Create(
                    batchCode: BatchCode, productCode: "STANDARD-90", kitCount: 7, adminId: 10001);

                var kits = new[]
                {
                    CreateKit("CDK-PRV2-0001", BatchCode),
                    CreateKit("CDK-PRV2-0002", BatchCode),
                    CreateKit("CDK-PRV2-0003", BatchCode),
                    CreateKit("CDK-PRV2-0004", BatchCode),
                    CreateKit("CDK-PRV2-0005", BatchCode),
                    CreateKit("CDK-PRV2-0006", BatchCode),
                    CreateKit("CDK-PRV2-0007", BatchCode),
                };

                foreach (var kit in kits)
                    kit.AssignToProvider(Provider2, SalesChannel.ConsignmentSellThrough, CargoDryCommercialModel.PrincipalSale);

                kits[2].Activate(10021, 21, 120);
                kits[3].Activate(10022, 22, 120);
                kits[4].Activate(10023, 23, 20);
                kits[5].Activate(10024, 24, 5);
                kits[6].Revoke("Provider demo revoke");

                _db.Batches.Add(batch);
                await _db.Kits.AddRangeAsync(kits, ct);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Seeded CargoDry provider2 kits (7).");
            }

            // ── Inventory row + movement ledger (skip if already seeded) ─────
            if (!await _db.ProviderInventories.AnyAsync(pi => pi.ProviderProfileId == Provider2, ct))
            {
                var inventory = CargoDryProviderInventoryEntity.Create(
                    Provider2, "STANDARD-90", BatchCode,
                    CargoDryCommercialModel.PrincipalSale, SalesChannel.ConsignmentSellThrough,
                    StockLocationType.ProviderWarehouse, 7, nowUtc);
                inventory.IncrementActivated(4, nowUtc);
                inventory.IncrementRevoked(1, nowUtc);
                _db.ProviderInventories.Add(inventory);

                var baseTime = nowUtc.AddMinutes(-30);
                var movements = new[]
                {
                    CargoDryInventoryMovementEntity.Create(Provider2, "STANDARD-90", InventoryMovementType.BatchAllocated,
                        7, baseTime, BatchCode, balanceAfter: 7,
                        commercialModel: CargoDryCommercialModel.PrincipalSale, salesChannel: SalesChannel.ConsignmentSellThrough,
                        note: "Batch allocated to provider (demo seed)"),
                    CargoDryInventoryMovementEntity.Create(Provider2, "STANDARD-90", InventoryMovementType.KitActivated,
                        -1, baseTime.AddMinutes(5), BatchCode, balanceAfter: 6,
                        commercialModel: CargoDryCommercialModel.PrincipalSale, salesChannel: SalesChannel.ConsignmentSellThrough,
                        note: "CDK-PRV2-0003"),
                    CargoDryInventoryMovementEntity.Create(Provider2, "STANDARD-90", InventoryMovementType.KitActivated,
                        -1, baseTime.AddMinutes(10), BatchCode, balanceAfter: 5,
                        commercialModel: CargoDryCommercialModel.PrincipalSale, salesChannel: SalesChannel.ConsignmentSellThrough,
                        note: "CDK-PRV2-0004"),
                    CargoDryInventoryMovementEntity.Create(Provider2, "STANDARD-90", InventoryMovementType.KitActivated,
                        -1, baseTime.AddMinutes(15), BatchCode, balanceAfter: 4,
                        commercialModel: CargoDryCommercialModel.PrincipalSale, salesChannel: SalesChannel.ConsignmentSellThrough,
                        note: "CDK-PRV2-0005"),
                    CargoDryInventoryMovementEntity.Create(Provider2, "STANDARD-90", InventoryMovementType.KitActivated,
                        -1, baseTime.AddMinutes(20), BatchCode, balanceAfter: 3,
                        commercialModel: CargoDryCommercialModel.PrincipalSale, salesChannel: SalesChannel.ConsignmentSellThrough,
                        note: "CDK-PRV2-0006"),
                    CargoDryInventoryMovementEntity.Create(Provider2, "STANDARD-90", InventoryMovementType.KitRevoked,
                        -1, baseTime.AddMinutes(25), BatchCode, balanceAfter: 2,
                        commercialModel: CargoDryCommercialModel.PrincipalSale, salesChannel: SalesChannel.ConsignmentSellThrough,
                        note: "CDK-PRV2-0007 — Provider demo revoke"),
                };
                await _db.InventoryMovements.AddRangeAsync(movements, ct);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Seeded CargoDry provider2 inventory row + 6 movements.");
            }

            // ── Sales attributions + settlement (skip if already seeded) ────
            if (!await _db.SalesAttributions.AnyAsync(a => a.ProviderProfileId == Provider2, ct))
            {
                // Ensure provider2 has a consignment agreement (FK target for settlement)
                var agreement = await _db.ConsignmentAgreements
                    .FirstOrDefaultAsync(a => a.ProviderProfileId == Provider2 && a.ProductCode == "STANDARD-90", ct);
                if (agreement is null)
                {
                    agreement = CargoDryConsignmentAgreementEntity.Create(
                        agreementCode: "CONS-PRV2-STD90", providerProfileId: Provider2, productCode: "STANDARD-90",
                        consignmentRate: 0.20m, minimumSettlementAmount: 0m, currencyCode: "USD",
                        maxKitCount: 100, startDateUtc: nowUtc);
                    await _db.ConsignmentAgreements.AddAsync(agreement, ct);
                    await _db.SaveChangesAsync(ct);
                }

                foreach (var code in new[] { "CDK-PRV2-0003", "CDK-PRV2-0004", "CDK-PRV2-0005" })
                {
                    var kit = await _db.Kits.FirstOrDefaultAsync(k => k.KitCode == code, ct);
                    if (kit is null) continue;
                    var attr = CargoDrySalesAttributionEntity.Create(
                        kitId: kit.Id, serialNumber: kit.SerialNumber, kitCode: kit.KitCode,
                        productCode: "STANDARD-90", batchCode: BatchCode,
                        salesChannel: SalesChannel.ConsignmentSellThrough,
                        commercialModel: CargoDryCommercialModel.PrincipalSale,
                        initialStatus: CargoDrySalesAttributionStatus.Attributed,
                        nowUtc: nowUtc, providerProfileId: Provider2,
                        consignmentAgreementId: agreement.Id);
                    attr.ResolveFinancials(
                        salePrice: 149.99m, commissionRate: 0.20m, currencyCode: "USD",
                        resolvedAtUtc: nowUtc, resolvedByUserId: 10001);
                    await _db.SalesAttributions.AddAsync(attr, ct);
                }

                var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1);
                var settlement = CargoDrySellThroughSettlementEntity.Create(
                    settlementCode: $"STL-{nowUtc:yyyyMM}-PRV2", consignmentAgreementId: agreement.Id,
                    providerProfileId: Provider2, productCode: "STANDARD-90", batchCode: BatchCode,
                    currencyCode: "USD", periodStartUtc: monthStart, periodEndUtc: monthEnd, nowUtc: nowUtc);
                settlement.RecalculateTotals(
                    totalKitCount: 2, totalSaleAmount: 299.98m, totalProviderShareAmount: 60m);
                await _db.SellThroughSettlements.AddAsync(settlement, ct);

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Seeded CargoDry provider2 sales attributions (3) + settlement (1).");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CargoDryProviderMockSeed failed (non-fatal): {Inner}", ex.InnerException?.Message);
            _db.ChangeTracker.Clear();
        }
    }

    private static CargoDryKitEntity CreateKit(string serial, string batchCode)
    {
        return CargoDryKitEntity.Create(
            serialNumber: serial,
            kitCode: serial,
            productCode: "STANDARD-90",
            batchCode: batchCode,
            qrPayload: $"https://cargodry.inktavia.com/kit/{serial}");
    }
}
