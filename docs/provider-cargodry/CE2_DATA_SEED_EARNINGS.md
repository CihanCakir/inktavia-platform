# CE-2 (data) — seed realized commission for provider2 so the earnings cockpit lights up

The earnings endpoint works, but `thisMonthCommission` / `ytdCommission` / `pendingPayout` are **0**: provider2's
activated kits (CI-1c seed) were created with `AssignToProvider` + `Activate` directly — they never produced
`CargoDrySalesAttribution` (per-sale commission) or `CargoDrySellThroughSettlement` (payout) records. Seed a small,
realistic set so the cockpit shows real "earned" numbers. Idempotent, dev-only. Extend `CargoDryProviderMockSeed`.

## Target end-state (provider2 = 100011)
- **3 sales attributions** (this calendar month) for 3 of the activated STANDARD-90 kits → `thisMonthCommission` =
  `ytdCommission` = **90 USD** (3 × provider share 30), `avgEarningPerKit` = 90 / soldKits(4) ≈ 22.5.
- **1 settlement (Pending)** with `ProviderPayoutAmount` = **60 USD** → `pendingPayout` = 60.
- (`paidPayout` stays 0 — realistic, and the cockpit doesn't display it.)

> The earnings handler sums `SalesAttribution.ProviderShareAmount` (set by `ResolveFinancials`) for the period, and
> `SellThroughSettlement.ProviderPayoutAmount` where Status ∈ {Pending, ReadyForSettlement, Scheduled}.

## Extend `CargoDryProviderMockSeed.SeedAsync` — new guarded block
Inject `ICargoDrySalesAttributionRepository` + `ICargoDrySellThroughSettlementRepository` (or use `_db` DbSets
`SalesAttributions` / `SellThroughSettlements`). Guard independently so it runs on the current DB (kits already
present):
```csharp
const long Provider2 = 100011;
if (!await _db.SalesAttributions.AnyAsync(a => a.ProviderProfileId == Provider2, ct))
{
    var now = DateTime.UtcNow;
    // look up the 3 activated kits by code (they were seeded earlier; fetch to get Id + SerialNumber)
    foreach (var code in new[] { "CDK-PRV2-0003", "CDK-PRV2-0004", "CDK-PRV2-0005" })
    {
        var kit = await _db.Kits.FirstOrDefaultAsync(k => k.KitCode == code, ct);
        if (kit is null) continue;
        var attr = CargoDrySalesAttributionEntity.Create(
            kitId: kit.Id, serialNumber: kit.SerialNumber, kitCode: kit.KitCode,
            productCode: "STANDARD-90", batchCode: "202507-CONS-PRV2",
            salesChannel: SalesChannel.ConsignmentSellThrough,
            commercialModel: CargoDryCommercialModel.PrincipalSale,
            initialStatus: CargoDrySalesAttributionStatus.Attributed,
            nowUtc: now, providerProfileId: Provider2);
        attr.ResolveFinancials(
            salePrice: 149.99m, commissionRate: 0.20m, currencyCode: "USD",
            resolvedAtUtc: now, resolvedByUserId: 10001);   // providerShare = 30.00 each
        await _db.SalesAttributions.AddAsync(attr, ct);
    }

    // one Pending settlement for the current month → pendingPayout = 60
    var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    var monthEnd   = monthStart.AddMonths(1);
    var settlement = CargoDrySellThroughSettlementEntity.Create(
        settlementCode: $"STL-{now:yyyyMM}-PRV2", consignmentAgreementId: 0,
        providerProfileId: Provider2, productCode: "STANDARD-90", batchCode: "202507-CONS-PRV2",
        currencyCode: "USD", periodStartUtc: monthStart, periodEndUtc: monthEnd, nowUtc: now);
    settlement.RecalculateTotals(totalProviderShareAmount: 60m);   // Status stays Pending
    await _db.SellThroughSettlements.AddAsync(settlement, ct);

    await _db.SaveChangesAsync(ct);
}
```
(Adjust the exact `RecalculateTotals` signature to match the entity — the goal is `ProviderPayoutAmount == 60m` on a
Pending settlement.) Keep it boot-safe (existing try/catch). Do not touch the kit/inventory/movement blocks.

## Acceptance
Rebuild + restart **cargodry-api**, then `GET /provider/cargodry/earnings` (provider2):
- `thisMonthCommission` = 90, `ytdCommission` = 90, `avgEarningPerKit` ≈ 22.5, `pendingPayout` = 60.
- `inHandPotential` = 60, `renewalPotential` = 60, `sellThroughPct` = 57.1 (unchanged).
- Re-running the seed does not duplicate (guard on existing provider2 attribution).

## Report
`REPORT_BACKEND.md` ("CE-2 data"): seeded 3 sales attributions (provider share 30 each, this month) + 1 Pending
settlement (payout 60) for provider2, so the earnings cockpit shows real this-month commission + pending payout.
