using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryConsignmentAgreementRepository : ICargoDryConsignmentAgreementRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryConsignmentAgreementRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDryConsignmentAgreementEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.ConsignmentAgreements.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CargoDryConsignmentAgreementEntity?> GetByAgreementCodeAsync(
        string agreementCode, CancellationToken ct)
        => _db.ConsignmentAgreements
            .FirstOrDefaultAsync(x => x.AgreementCode == agreementCode, ct);

    public Task<CargoDryConsignmentAgreementEntity?> GetActiveForProviderProductAsync(
        long providerProfileId, string productCode, DateTime nowUtc, CancellationToken ct)
        => _db.ConsignmentAgreements
            .FirstOrDefaultAsync(x =>
                x.ProviderProfileId == providerProfileId
                && x.ProductCode    == productCode
                && x.Status         == ConsignmentAgreementStatus.Active
                && x.StartDateUtc   <= nowUtc
                && (x.EndDateUtc == null || x.EndDateUtc >= nowUtc),
            ct);

    public Task<bool> ExistsActiveForProviderProductAsync(
        long providerProfileId, string productCode, long? excludeId, CancellationToken ct)
        => _db.ConsignmentAgreements
            .AnyAsync(x =>
                x.ProviderProfileId == providerProfileId
                && x.ProductCode    == productCode
                && x.Status         == ConsignmentAgreementStatus.Active
                && (excludeId == null || x.Id != excludeId.Value),
            ct);

    public async Task<(List<CargoDryConsignmentAgreementEntity> Items, int Total)> GetPagedAsync(
        long?                       providerProfileId,
        string?                     productCode,
        ConsignmentAgreementStatus? status,
        DateTime?                   dateFrom,
        DateTime?                   dateTo,
        string?                     search,
        int                         skip,
        int                         take,
        CancellationToken           ct)
    {
        var query = _db.ConsignmentAgreements.AsNoTracking().AsQueryable();

        if (providerProfileId.HasValue)
            query = query.Where(x => x.ProviderProfileId == providerProfileId.Value);

        if (!string.IsNullOrWhiteSpace(productCode))
            query = query.Where(x => x.ProductCode == productCode);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (dateFrom.HasValue)
            query = query.Where(x => x.StartDateUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.StartDateUtc <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.AgreementCode.Contains(search) ||
                x.ProductCode.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<long>> GetDistinctActiveProviderProfileIdsAsync(
        CancellationToken ct)
    {
        var ids = await _db.ConsignmentAgreements
            .Where(x => x.Status == ConsignmentAgreementStatus.Active)
            .Select(x => x.ProviderProfileId)
            .Distinct()
            .ToListAsync(ct);

        return ids;
    }

    public async Task AddAsync(CargoDryConsignmentAgreementEntity entity, CancellationToken ct)
    {
        await _db.ConsignmentAgreements.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
