# CE-2 data FIX — settlement seed fails on ConsignmentAgreementId FK (0)

The provider2 attribution/settlement seed now runs but `SaveChangesAsync` (line 136) throws (non-fatal catch →
nothing persists, so even the 3 attributions roll back and the cockpit stays 0):
```
CargoDryProviderMockSeed failed (non-fatal). ... SeedAsync ... line 136
```
Root cause: the settlement's `ConsignmentAgreementId` is `IsRequired()` **and FK-constrained**, but the seed passes
`consignmentAgreementId: 0` — provider2 has no consignment agreement, so `0` violates the FK and the whole
`SaveChanges` (attributions + settlement together) rolls back.

## Fix — seed a consignment agreement for provider2, use its id
In `CargoDryProviderMockSeed`, inside the same attribution guard block (`!SalesAttributions.Any(100011)`), create the
agreement first, save it to get its Id, then reference that Id on the settlement (and, ideally, on the attributions).

```csharp
// 0) ensure provider2 has a consignment agreement (FK target for the settlement)
var agreement = await _db.ConsignmentAgreements
    .FirstOrDefaultAsync(a => a.ProviderProfileId == Provider2 && a.ProductCode == "STANDARD-90", ct);
if (agreement is null)
{
    agreement = CargoDryConsignmentAgreementEntity.Create(
        agreementCode: "CONS-PRV2-STD90", providerProfileId: Provider2, productCode: "STANDARD-90",
        consignmentRate: 0.20m, minimumSettlementAmount: 0m, currencyCode: "USD",
        maxKitCount: 100, startDateUtc: nowUtc);
    await _db.ConsignmentAgreements.AddAsync(agreement, ct);
    await _db.SaveChangesAsync(ct);   // commit so agreement.Id is assigned
}

// 1) attributions (link to the agreement) ...
//    CargoDrySalesAttributionEntity.Create(..., consignmentAgreementId: agreement.Id, ...)
// 2) settlement ...
//    CargoDrySellThroughSettlementEntity.Create(settlementCode: $"STL-{nowUtc:yyyyMM}-PRV2",
//        consignmentAgreementId: agreement.Id, ...)   // <-- was 0
```
- Replace the settlement's `consignmentAgreementId: 0` with `agreement.Id`.
- Pass `consignmentAgreementId: agreement.Id` on the 3 attribution `Create(...)` calls too (consistent; the field is
  nullable there but linking is cleaner).
- Confirm `_db.ConsignmentAgreements` is the correct DbSet name (else use the agreement repository's Add).
- Keep the block idempotent (the outer `!SalesAttributions.Any(100011)` guard already covers re-runs; the agreement
  has its own `FirstOrDefault` guard).

## Nice-to-have (diagnostics)
In the seed's `catch`, log `ex.InnerException?.Message` alongside the stack so future seed failures show the real DB
error, not just the line number.

## Acceptance
Rebuild + `up -d --force-recreate cargodry-api`, then `GET /provider/cargodry/earnings` (provider2):
- `thisMonthCommission` = 90, `ytdCommission` = 90, `avgEarningPerKit` ≈ 22.5, `pendingPayout` = 60.
- Logs show "Seeded CargoDry provider2 sales attributions (3) + settlement (1)." and **no** "failed (non-fatal)".

## Report
`REPORT_BACKEND.md` ("CE-2 data fix"): seeded a provider2 consignment agreement and referenced its id on the
settlement (+attributions), fixing the ConsignmentAgreementId FK violation that rolled back the earnings seed.
