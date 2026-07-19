using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryKitRepository : ICargoDryKitRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryKitRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDryKitEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Kits.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CargoDryKitEntity?> GetBySerialAsync(string serialNumber, CancellationToken ct)
        => _db.Kits.FirstOrDefaultAsync(x => x.SerialNumber == serialNumber, ct);

    public Task<List<CargoDryKitEntity>> GetByOwnerAsync(long userId, CancellationToken ct)
        => _db.Kits
            .Where(x => x.OwnerUserId == userId)
            .OrderByDescending(x => x.ActivatedAt)
            .ToListAsync(ct);

    public Task<CargoDryKitEntity?> GetActiveByVesselAsync(
        long vesselId, string productCode, CancellationToken ct)
        => _db.Kits.FirstOrDefaultAsync(x =>
            x.VesselId == vesselId &&
            x.ProductCode == productCode &&
            x.Status == CargoDryKitStatus.Activated, ct);

    public Task<List<CargoDryKitEntity>> GetExpiringAsync(int withinDays, long? providerProfileId = null, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(withinDays);
        var q = _db.Kits
            .Where(x => x.Status == CargoDryKitStatus.Activated
                     && x.ExpiresAt.HasValue
                     && x.ExpiresAt.Value <= cutoff
                     && x.ExpiresAt.Value > DateTimeOffset.UtcNow);
        if (providerProfileId.HasValue) q = q.Where(x => x.ProviderProfileId == providerProfileId.Value);
        return q.ToListAsync(ct);
    }

    public Task<List<CargoDryKitEntity>> GetExpiredUnmarkedAsync(long? providerProfileId = null, CancellationToken ct = default)
    {
        var q = _db.Kits
            .Where(x => x.Status == CargoDryKitStatus.Activated
                     && x.ExpiresAt.HasValue
                     && x.ExpiresAt.Value < DateTimeOffset.UtcNow);
        if (providerProfileId.HasValue) q = q.Where(x => x.ProviderProfileId == providerProfileId.Value);
        return q.ToListAsync(ct);
    }

    public async Task<(List<CargoDryKitEntity> Items, int Total)> GetPagedAsync(
        CargoDryKitStatus? status, string? search, long? vesselId, long? ownerUserId, string? batchCode, int skip, int take, long? providerProfileId = null, CancellationToken ct = default)
    {
        var q = _db.Kits.AsQueryable();
        if (providerProfileId.HasValue)             q = q.Where(x => x.ProviderProfileId == providerProfileId.Value);
        if (status.HasValue)                        q = q.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))     q = q.Where(x => x.SerialNumber.Contains(search) || x.KitCode.Contains(search));
        if (vesselId.HasValue)                      q = q.Where(x => x.VesselId == vesselId.Value);
        if (ownerUserId.HasValue)                   q = q.Where(x => x.OwnerUserId == ownerUserId.Value);
        if (!string.IsNullOrWhiteSpace(batchCode))  q = q.Where(x => x.BatchCode == batchCode);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.ManufacturedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public Task<List<CargoDryKitEntity>> GetAllAsync(CancellationToken ct)
        => _db.Kits.AsNoTracking().OrderByDescending(x => x.ManufacturedAt).ToListAsync(ct);

    public Task<List<CargoDryKitEntity>> GetAllForReportAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => _db.Kits
            .AsNoTracking()
            .Where(k => (k.ManufacturedAt >= from && k.ManufacturedAt <= to)
                     || (k.ActivatedAt.HasValue && k.ActivatedAt >= from && k.ActivatedAt <= to))
            .OrderByDescending(k => k.ManufacturedAt)
            .ToListAsync(ct);

    public async Task<CargoDryStatsProjection> GetStatsAsync(long? providerProfileId = null, CancellationToken ct = default)
    {
        var utcNow     = DateTimeOffset.UtcNow;
        var in30Days   = utcNow.AddDays(30);
        var todayStart = new DateTimeOffset(utcNow.Date, TimeSpan.Zero);
        var todayEnd   = todayStart.AddDays(1);

        IQueryable<CargoDryKitEntity> q = _db.Kits;
        if (providerProfileId.HasValue) q = q.Where(x => x.ProviderProfileId == providerProfileId.Value);

        return new CargoDryStatsProjection
        {
            Total     = await q.CountAsync(ct),
            Available = await q.CountAsync(x => x.Status == CargoDryKitStatus.Available, ct),
            Active    = await q.CountAsync(x => x.Status == CargoDryKitStatus.Activated, ct),
            Expiring  = await q.CountAsync(x =>
                x.Status == CargoDryKitStatus.Activated &&
                x.ExpiresAt.HasValue && x.ExpiresAt.Value <= in30Days, ct),
            Expired   = await q.CountAsync(x => x.Status == CargoDryKitStatus.Expired, ct),
            Revoked   = await q.CountAsync(x => x.Status == CargoDryKitStatus.Revoked, ct),
            TodayActivations = await q.CountAsync(x =>
                x.ActivatedAt.HasValue &&
                x.ActivatedAt.Value >= todayStart &&
                x.ActivatedAt.Value < todayEnd, ct),
            WithRenewals = await q.CountAsync(x => x.RenewalCount > 0, ct),
        };
    }

    public async Task<CargoDryProductKitStatsProjection> GetKitStatsByProductCodeAsync(
        string productCode, CancellationToken ct)
    {
        var utcNow   = DateTimeOffset.UtcNow;
        var in30Days = utcNow.AddDays(30);

        var q = _db.Kits.AsNoTracking().Where(k => k.ProductCode == productCode);

        var total        = await q.CountAsync(ct);
        var active       = await q.CountAsync(k => k.Status == CargoDryKitStatus.Activated, ct);
        var expired      = await q.CountAsync(k => k.Status == CargoDryKitStatus.Expired, ct);
        var revoked      = await q.CountAsync(k => k.Status == CargoDryKitStatus.Revoked, ct);
        var renewed      = await q.CountAsync(k => k.RenewalCount > 0, ct);
        var expIn30      = await q.CountAsync(k =>
            k.Status == CargoDryKitStatus.Activated &&
            k.ExpiresAt.HasValue && k.ExpiresAt.Value <= in30Days, ct);

        // AvgEfficiency: computed from ActivatedAt/ExpiresAt — fetch only active kits' date pair.
        double avgEff = 0d;
        if (active > 0)
        {
            var activeDates = await q
                .Where(k => k.Status == CargoDryKitStatus.Activated && k.ActivatedAt.HasValue && k.ExpiresAt.HasValue)
                .Select(k => new { k.ActivatedAt, k.ExpiresAt })
                .ToListAsync(ct);

            if (activeDates.Count > 0)
            {
                var effSum = activeDates.Sum(d =>
                {
                    var total2 = (d.ExpiresAt!.Value - d.ActivatedAt!.Value).TotalDays;
                    if (total2 <= 0) return 0d;
                    var remaining = (d.ExpiresAt.Value - utcNow).TotalDays;
                    return Math.Max(0, Math.Min(100, (1 - remaining / total2) * 100));
                });
                avgEff = Math.Round(effSum / activeDates.Count, 1);
            }
        }

        // RenewalRate = renewed / ever-activated kits * 100
        var activated = await q.CountAsync(k => k.Status != CargoDryKitStatus.Available, ct);
        var renewalRate = activated > 0
            ? Math.Round(renewed / (double)activated * 100d, 1)
            : 0d;

        return new CargoDryProductKitStatsProjection
        {
            TotalKits            = total,
            ActiveKits           = active,
            ExpiredKits          = expired,
            RevokedKits          = revoked,
            RenewedKits          = renewed,
            ExpiringIn30Days     = expIn30,
            AvgEfficiencyPercent = avgEff,
            RenewalRatePercent   = renewalRate,
        };
    }

    public Task<CargoDryKitEntity?> GetByKitCodeAsync(string kitCode, CancellationToken ct)
        => _db.Kits.FirstOrDefaultAsync(x => x.KitCode == kitCode, ct);

    public Task<List<CargoDryKitEntity>> GetAvailableByBatchCodeAsync(string batchCode, CancellationToken ct)
        => _db.Kits
            .Where(x => x.BatchCode == batchCode && x.Status == CargoDryKitStatus.Available)
            .ToListAsync(ct);

    public async Task AddAsync(CargoDryKitEntity entity, CancellationToken ct)
    {
        await _db.Kits.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<CargoDryKitEntity> entities, CancellationToken ct)
    {
        await _db.Kits.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
