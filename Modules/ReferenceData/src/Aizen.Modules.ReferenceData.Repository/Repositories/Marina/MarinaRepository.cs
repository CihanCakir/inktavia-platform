using Aizen.Modules.ReferenceData.Domain.Entities.Marina;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Repositories.Marina;

public sealed class MarinaRepository : IMarinaRepository
{
    private readonly ReferenceDataDbContext _dbContext;

    public MarinaRepository(ReferenceDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<MarinaEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _dbContext.Marinas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<MarinaEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.Marinas.FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.Marinas.AnyAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public Task AddAsync(MarinaEntity entity, CancellationToken cancellationToken = default)
        => _dbContext.Marinas.AddAsync(entity, cancellationToken).AsTask();

    public void Update(MarinaEntity entity)
        => _dbContext.Marinas.Update(entity);

    public async Task<(IReadOnlyList<MarinaEntity> Items, int Total)> ListForAdminAsync(
        bool needsReviewOnly, string? search, int skip, int take, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Marinas.AsNoTracking();
        if (needsReviewOnly) q = q.Where(x => x.NeedsReview);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(x =>
                x.Name.ToLower().Contains(term) ||
                (x.CityCode != null && x.CityCode.ToLower().Contains(term)) ||
                (x.Province != null && x.Province.ToLower().Contains(term)));
        }
        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.NeedsReview)   // review queue first
            .ThenBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
