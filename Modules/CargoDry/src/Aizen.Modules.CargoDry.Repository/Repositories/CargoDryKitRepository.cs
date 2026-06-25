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

    public Task<List<CargoDryKitEntity>> GetExpiringAsync(int withinDays, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(withinDays);
        return _db.Kits
            .Where(x => x.Status == CargoDryKitStatus.Activated
                     && x.ExpiresAt.HasValue
                     && x.ExpiresAt.Value <= cutoff
                     && x.ExpiresAt.Value > DateTimeOffset.UtcNow)
            .ToListAsync(ct);
    }

    public Task<List<CargoDryKitEntity>> GetExpiredUnmarkedAsync(CancellationToken ct)
        => _db.Kits
            .Where(x => x.Status == CargoDryKitStatus.Activated
                     && x.ExpiresAt.HasValue
                     && x.ExpiresAt.Value < DateTimeOffset.UtcNow)
            .ToListAsync(ct);

    public async Task<(List<CargoDryKitEntity> Items, int Total)> GetPagedAsync(
        CargoDryKitStatus? status, string? search, long? vesselId, int skip, int take, CancellationToken ct)
    {
        var q = _db.Kits.AsQueryable();
        if (status.HasValue)                    q = q.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.SerialNumber.Contains(search) || x.KitCode.Contains(search));
        if (vesselId.HasValue)                  q = q.Where(x => x.VesselId == vesselId.Value);
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

    public async Task<CargoDryStatsProjection> GetStatsAsync(CancellationToken ct)
    {
        var in30Days = DateTimeOffset.UtcNow.AddDays(30);
        var todayStart = DateTimeOffset.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        return new CargoDryStatsProjection
        {
            Total     = await _db.Kits.CountAsync(ct),
            Available = await _db.Kits.CountAsync(x => x.Status == CargoDryKitStatus.Available, ct),
            Active    = await _db.Kits.CountAsync(x => x.Status == CargoDryKitStatus.Activated, ct),
            Expiring  = await _db.Kits.CountAsync(x =>
                x.Status == CargoDryKitStatus.Activated &&
                x.ExpiresAt.HasValue && x.ExpiresAt.Value <= in30Days, ct),
            Expired   = await _db.Kits.CountAsync(x => x.Status == CargoDryKitStatus.Expired, ct),
            Revoked   = await _db.Kits.CountAsync(x => x.Status == CargoDryKitStatus.Revoked, ct),
            TodayActivations = await _db.Kits.CountAsync(x =>
                x.ActivatedAt.HasValue &&
                x.ActivatedAt.Value >= todayStart &&
                x.ActivatedAt.Value < todayEnd, ct),
        };
    }

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
