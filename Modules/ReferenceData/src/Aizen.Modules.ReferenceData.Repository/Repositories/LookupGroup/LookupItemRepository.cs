using Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Repositories.LookupGroup;

public sealed class LookupItemRepository : ILookupItemRepository
{
    private readonly ReferenceDataDbContext _dbContext;

    public LookupItemRepository(ReferenceDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LookupItemEntity?> GetItemByIdAsync(long id, CancellationToken cancellationToken = default)
        => _dbContext.LookupItems.Include(x => x.LookupGroup).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<LookupItemEntity?> GetItemByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.LookupItems.Include(x => x.LookupGroup).FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemEntity>> GetItemsByGroupIdAsync(long lookupGroupId, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LookupItems.AsNoTracking().Include(x => x.LookupGroup).Where(x => x.LookupGroupId == lookupGroupId);
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemEntity>> GetItemsByGroupCodeAsync(string groupCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var normalizedCode = groupCode.Trim().ToUpperInvariant();
        var query = _dbContext.LookupItems.AsNoTracking().Include(x => x.LookupGroup).Where(x => x.LookupGroup.Code == normalizedCode);
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeInGroupAsync(long lookupGroupId, string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.LookupItems.AnyAsync(x => x.LookupGroupId == lookupGroupId && x.Code == normalizedCode, cancellationToken);
    }

    public Task AddAsync(LookupItemEntity entity, CancellationToken cancellationToken = default)
        => _dbContext.LookupItems.AddAsync(entity, cancellationToken).AsTask();

    public void Update(LookupItemEntity entity)
        => _dbContext.LookupItems.Update(entity);
}
