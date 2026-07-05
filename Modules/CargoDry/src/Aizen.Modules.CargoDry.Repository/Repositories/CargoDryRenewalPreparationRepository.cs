using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryRenewalPreparationRepository : ICargoDryRenewalPreparationRepository
{
    private readonly CargoDryDbContext _db;

    public CargoDryRenewalPreparationRepository(CargoDryDbContext db)
        => _db = db;

    private static readonly CargoDryRenewalPreparationStatus[] TerminalStatuses =
    [
        CargoDryRenewalPreparationStatus.Completed,
        CargoDryRenewalPreparationStatus.Cancelled,
        CargoDryRenewalPreparationStatus.Failed,
    ];

    public Task<CargoDryRenewalPreparationEntity?> GetByIdAsync(
        long id, CancellationToken ct = default)
        => _db.RenewalPreparations
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

    public Task<CargoDryRenewalPreparationEntity?> GetByRenewalCodeAsync(
        string renewalCode, CancellationToken ct = default)
        => _db.RenewalPreparations
            .FirstOrDefaultAsync(x => x.RenewalCode == renewalCode && x.IsActive, ct);

    public Task<CargoDryRenewalPreparationEntity?> GetOpenForKitAsync(
        long kitId, CancellationToken ct = default)
        => _db.RenewalPreparations
            .FirstOrDefaultAsync(
                x => x.KitId == kitId
                  && x.IsActive
                  && !TerminalStatuses.Contains(x.Status),
                ct);

    public async Task<(List<CargoDryRenewalPreparationEntity> Items, int Total)> GetPagedAsync(
        long?                              kitId,
        string?                            kitCode,
        string?                            productCode,
        long?                              ownerUserId,
        long?                              vesselId,
        CargoDryRenewalPreparationStatus?  status,
        CargoDryRenewalNotificationStatus? notificationStatus,
        DateTimeOffset?                    preparedFrom,
        DateTimeOffset?                    preparedTo,
        int                                skip,
        int                                take,
        CancellationToken                  ct = default)
    {
        var q = _db.RenewalPreparations.Where(x => x.IsActive);

        if (kitId.HasValue)
            q = q.Where(x => x.KitId == kitId.Value);

        if (!string.IsNullOrWhiteSpace(kitCode))
            q = q.Where(x => x.KitCode == kitCode);

        if (!string.IsNullOrWhiteSpace(productCode))
            q = q.Where(x => x.ProductCode == productCode);

        if (ownerUserId.HasValue)
            q = q.Where(x => x.OwnerUserId == ownerUserId.Value);

        if (vesselId.HasValue)
            q = q.Where(x => x.VesselId == vesselId.Value);

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (notificationStatus.HasValue)
            q = q.Where(x => x.NotificationStatus == notificationStatus.Value);

        if (preparedFrom.HasValue)
            q = q.Where(x => x.PreparedAtUtc >= preparedFrom.Value);

        if (preparedTo.HasValue)
            q = q.Where(x => x.PreparedAtUtc <= preparedTo.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.PreparedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<List<long>> GetKitIdsWithOpenPreparationAsync(
        CancellationToken ct = default)
        => await _db.RenewalPreparations
            .Where(x => x.IsActive && !TerminalStatuses.Contains(x.Status))
            .Select(x => x.KitId)
            .Distinct()
            .ToListAsync(ct);

    public async Task AddAsync(CargoDryRenewalPreparationEntity entity, CancellationToken ct = default)
        => await _db.RenewalPreparations.AddAsync(entity, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
