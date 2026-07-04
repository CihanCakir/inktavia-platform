using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDrySettlementAutomationRunRepository : ICargoDrySettlementAutomationRunRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDrySettlementAutomationRunRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDrySettlementAutomationRunEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.SettlementAutomationRuns
            .Include(x => x.RunItems)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CargoDrySettlementAutomationRunEntity?> GetByCodeAsync(string runCode, CancellationToken ct)
        => _db.SettlementAutomationRuns
            .Include(x => x.RunItems)
            .FirstOrDefaultAsync(x => x.RunCode == runCode, ct);

    public async Task<(List<CargoDrySettlementAutomationRunEntity> Items, int Total)> GetPagedAsync(
        int?                                    targetYearMonth,
        CargoDrySettlementAutomationRunStatus?  status,
        CargoDrySettlementAutomationMode?       mode,
        long?                                   triggeredByUserId,
        DateTime?                               fromUtc,
        DateTime?                               toUtc,
        int                                     skip,
        int                                     take,
        CancellationToken                       ct)
    {
        var query = _db.SettlementAutomationRuns.AsNoTracking().AsQueryable();

        if (targetYearMonth.HasValue)
            query = query.Where(x => x.TargetYearMonth == targetYearMonth.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (mode.HasValue)
            query = query.Where(x => x.Mode == mode.Value);

        if (triggeredByUserId.HasValue)
            query = query.Where(x => x.TriggeredByUserId == triggeredByUserId.Value);

        if (fromUtc.HasValue)
            query = query.Where(x => x.TriggeredAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(x => x.TriggeredAtUtc <= toUtc.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.TriggeredAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> GetNextRunSequenceAsync(int targetYearMonth, CancellationToken ct)
    {
        var count = await _db.SettlementAutomationRuns
            .AsNoTracking()
            .CountAsync(x => x.TargetYearMonth == targetYearMonth, ct);
        return count + 1;
    }

    public async Task AddAsync(CargoDrySettlementAutomationRunEntity entity, CancellationToken ct)
        => await _db.SettlementAutomationRuns.AddAsync(entity, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
