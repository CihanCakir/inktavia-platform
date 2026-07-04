using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDrySellThroughSettlementRepository : ICargoDrySellThroughSettlementRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDrySellThroughSettlementRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDrySellThroughSettlementEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.SellThroughSettlements.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CargoDrySellThroughSettlementEntity?> GetByCodeAsync(
        string settlementCode, CancellationToken ct)
        => _db.SellThroughSettlements
            .FirstOrDefaultAsync(x => x.SettlementCode == settlementCode, ct);

    /// <inheritdoc cref="ICargoDrySellThroughSettlementRepository.GetOpenForProviderCurrencyProductPeriodAsync"/>
    public Task<CargoDrySellThroughSettlementEntity?> GetOpenForProviderCurrencyProductPeriodAsync(
        long              providerProfileId,
        string            currencyCode,
        string            productCode,
        DateTime          periodStartUtc,
        DateTime          periodEndUtc,
        CancellationToken ct)
        => _db.SellThroughSettlements
            .FirstOrDefaultAsync(x =>
                x.ProviderProfileId == providerProfileId &&
                x.CurrencyCode      == currencyCode      &&
                x.ProductCode       == productCode       &&
                x.PeriodStartUtc    == periodStartUtc    &&
                x.PeriodEndUtc      == periodEndUtc      &&
                x.Status            == CargoDrySellThroughSettlementStatus.Pending,
            ct);

    public async Task<(List<CargoDrySellThroughSettlementEntity> Items, int Total)> GetPagedAsync(
        long?                                providerProfileId,
        long?                                consignmentAgreementId,
        string?                              productCode,
        CargoDrySellThroughSettlementStatus? status,
        DateTime?                            periodFrom,
        DateTime?                            periodTo,
        string?                              search,
        int                                  skip,
        int                                  take,
        CancellationToken                    ct)
    {
        var query = _db.SellThroughSettlements.AsNoTracking().AsQueryable();

        if (providerProfileId.HasValue)
            query = query.Where(x => x.ProviderProfileId == providerProfileId.Value);

        if (consignmentAgreementId.HasValue)
            query = query.Where(x => x.ConsignmentAgreementId == consignmentAgreementId.Value);

        if (!string.IsNullOrWhiteSpace(productCode))
            query = query.Where(x => x.ProductCode == productCode);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (periodFrom.HasValue)
            query = query.Where(x => x.PeriodStartUtc >= periodFrom.Value);

        if (periodTo.HasValue)
            query = query.Where(x => x.PeriodEndUtc <= periodTo.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.SettlementCode.Contains(search) ||
                x.ProductCode.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <inheritdoc cref="ICargoDrySellThroughSettlementRepository.GetForYearMonthAsync"/>
    public Task<List<CargoDrySellThroughSettlementEntity>> GetForYearMonthAsync(
        int                                              targetYearMonth,
        IEnumerable<CargoDrySellThroughSettlementStatus> statuses,
        CancellationToken                                ct)
    {
        // Decompose YYYYMM → year + month for PeriodStartUtc comparison
        var year  = targetYearMonth / 100;
        var month = targetYearMonth % 100;
        var allowedStatuses = statuses.ToList();

        return _db.SellThroughSettlements
            .AsNoTracking()
            .Where(x =>
                x.PeriodStartUtc.Year  == year  &&
                x.PeriodStartUtc.Month == month &&
                allowedStatuses.Contains(x.Status))
            .OrderBy(x => x.SettlementCode)
            .ToListAsync(ct);
    }

    public async Task AddAsync(CargoDrySellThroughSettlementEntity entity, CancellationToken ct)
    {
        await _db.SellThroughSettlements.AddAsync(entity, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
