using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Repository.Seed;

/// <summary>
/// Seeds demo batches and kits for local/development environments.
/// Idempotent — skipped entirely if any kit already exists.
/// </summary>
public sealed class CargoDryBatchMockSeed
{
    private readonly CargoDryDbContext            _db;
    private readonly ILogger<CargoDryBatchMockSeed> _logger;

    public CargoDryBatchMockSeed(CargoDryDbContext db, ILogger<CargoDryBatchMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Kits.AnyAsync(ct))
        {
            _logger.LogDebug("CargoDry batch/kit seed skipped — data already present.");
            return;
        }

        // ── Batch 1: STANDARD-90, 4 kits, admin 10001 ─────────────────────────
        var batch1 = CargoDryBatchEntity.Create(
            batchCode:  "202506-STAN-DEV1",
            productCode: "STANDARD-90",
            kitCount:    4,
            adminId:     10001);

        // Kit 1 — Available (never activated)
        var kit1 = CargoDryKitEntity.Create(
            serialNumber: "CDK-STAN-0001",
            kitCode:      "CDK-STAN-0001",
            qrPayload:    "https://cargodry.inktavia.local/activate/CDK-STAN-0001",
            productCode:  "STANDARD-90",
            batchCode:    "202506-STAN-DEV1");

        // Kit 2 — Activated (owner: vessel owner 10002, vessel 1)
        var kit2 = CargoDryKitEntity.Create(
            serialNumber: "CDK-STAN-0002",
            kitCode:      "CDK-STAN-0002",
            qrPayload:    "https://cargodry.inktavia.local/activate/CDK-STAN-0002",
            productCode:  "STANDARD-90",
            batchCode:    "202506-STAN-DEV1");
        kit2.Activate(userId: 10002, vesselId: 1, validityDays: 90);

        // Kit 3 — Activated and renewed once (owner: 10003, vessel 2)
        var kit3 = CargoDryKitEntity.Create(
            serialNumber: "CDK-STAN-0003",
            kitCode:      "CDK-STAN-0003",
            qrPayload:    "https://cargodry.inktavia.local/activate/CDK-STAN-0003",
            productCode:  "STANDARD-90",
            batchCode:    "202506-STAN-DEV1");
        kit3.Activate(userId: 10003, vesselId: 2, validityDays: 90);
        kit3.Renew(additionalDays: 90, paymentRef: "PAY-LOCAL-DEV-001");

        // Kit 4 — Expired (simulated by activating with 0-day window)
        var kit4 = CargoDryKitEntity.Create(
            serialNumber: "CDK-STAN-0004",
            kitCode:      "CDK-STAN-0004",
            qrPayload:    "https://cargodry.inktavia.local/activate/CDK-STAN-0004",
            productCode:  "STANDARD-90",
            batchCode:    "202506-STAN-DEV1");
        kit4.Activate(userId: 10004, vesselId: 3, validityDays: 1);
        kit4.MarkExpired();

        // ── Batch 2: PREMIUM-180, 4 kits, admin 10001 ─────────────────────────
        var batch2 = CargoDryBatchEntity.Create(
            batchCode:   "202506-PREM-DEV1",
            productCode: "PREMIUM-180",
            kitCount:    4,
            adminId:     10001);

        // Kit 5 — Available
        var kit5 = CargoDryKitEntity.Create(
            serialNumber: "CDK-PREM-0001",
            kitCode:      "CDK-PREM-0001",
            qrPayload:    "https://cargodry.inktavia.local/activate/CDK-PREM-0001",
            productCode:  "PREMIUM-180",
            batchCode:    "202506-PREM-DEV1");

        // Kit 6 — Activated (owner: 10005, vessel 4)
        var kit6 = CargoDryKitEntity.Create(
            serialNumber: "CDK-PREM-0002",
            kitCode:      "CDK-PREM-0002",
            qrPayload:    "https://cargodry.inktavia.local/activate/CDK-PREM-0002",
            productCode:  "PREMIUM-180",
            batchCode:    "202506-PREM-DEV1");
        kit6.Activate(userId: 10005, vesselId: 4, validityDays: 180);

        // Kit 7 — Revoked
        var kit7 = CargoDryKitEntity.Create(
            serialNumber: "CDK-PREM-0003",
            kitCode:      "CDK-PREM-0003",
            qrPayload:    "https://cargodry.inktavia.local/activate/CDK-PREM-0003",
            productCode:  "PREMIUM-180",
            batchCode:    "202506-PREM-DEV1");
        kit7.Revoke("Demo revoke — local seed");

        // Kit 8 — Available (cargodry.team@inktavia.local admin user 10013)
        var kit8 = CargoDryKitEntity.Create(
            serialNumber: "CDK-PREM-0004",
            kitCode:      "CDK-PREM-0004",
            qrPayload:    "https://cargodry.inktavia.local/activate/CDK-PREM-0004",
            productCode:  "PREMIUM-180",
            batchCode:    "202506-PREM-DEV1");

        // ── Persist ────────────────────────────────────────────────────────────
        await _db.Batches.AddRangeAsync([batch1, batch2], ct);
        await _db.Kits.AddRangeAsync([kit1, kit2, kit3, kit4, kit5, kit6, kit7, kit8], ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CargoDry mock seed complete: 2 batches, 8 kits (2 Available, 2 Activated, 1 Renewed, 1 Expired, 1 Revoked, 1 Available).");
    }
}
