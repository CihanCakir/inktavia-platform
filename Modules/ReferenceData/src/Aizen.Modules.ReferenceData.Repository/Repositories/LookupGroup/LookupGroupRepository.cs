using Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Repositories.LookupGroup;

public sealed class LookupGroupRepository : ILookupGroupRepository
{
    private readonly ReferenceDataDbContext _dbContext;

    public LookupGroupRepository(ReferenceDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LookupGroupEntity?> GetGroupByIdAsync(long id, CancellationToken cancellationToken = default)
        => _dbContext.LookupGroups.Include(x => x.Children).Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<LookupGroupEntity?> GetGroupByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.LookupGroups.Include(x => x.Children).Include(x => x.Items).FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyList<LookupGroupEntity>> GetGroupsAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LookupGroups.AsNoTracking();
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.HierarchyPath).ThenBy(x => x.SortOrder).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupGroupEntity>> GetGroupTreeAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LookupGroups.AsNoTracking().Include(x => x.Items).AsQueryable();
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.HierarchyPath).ThenBy(x => x.SortOrder).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupGroupEntity>> GetChildrenAsync(long parentGroupId, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LookupGroups.AsNoTracking().Where(x => x.ParentLookupGroupId == parentGroupId);
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupGroupEntity>> GetDescendantsAsync(string hierarchyPath, CancellationToken cancellationToken = default)
    {
        var prefix = hierarchyPath.Trim() + "/";
        return await _dbContext.LookupGroups.Where(x => x.HierarchyPath.StartsWith(prefix)).OrderBy(x => x.HierarchyPath).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.LookupGroups.AnyAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public Task AddAsync(LookupGroupEntity entity, CancellationToken cancellationToken = default)
        => _dbContext.LookupGroups.AddAsync(entity, cancellationToken).AsTask();

    public void Update(LookupGroupEntity entity)
        => _dbContext.LookupGroups.Update(entity);
}
