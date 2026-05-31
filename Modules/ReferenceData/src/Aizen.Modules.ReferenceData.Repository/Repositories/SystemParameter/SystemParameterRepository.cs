using Aizen.Modules.ReferenceData.Domain.Entities.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Repositories.SystemParameter;

public sealed class SystemParameterRepository : ISystemParameterRepository
{
    private readonly ReferenceDataDbContext _dbContext;

    public SystemParameterRepository(ReferenceDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SystemParameterEntity?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalizedKey = key.Trim();
        return _dbContext.SystemParameters.FirstOrDefaultAsync(x => x.Key == normalizedKey, cancellationToken);
    }

    public async Task<IReadOnlyList<SystemParameterEntity>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SystemParameters.AsNoTracking();
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Key).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SystemParameterEntity>> GetByPrefixAsync(string prefix, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = prefix.Trim();
        var query = _dbContext.SystemParameters.AsNoTracking().Where(x => x.Key.StartsWith(normalizedPrefix));
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Key).ToListAsync(cancellationToken);
    }

    public Task AddAsync(SystemParameterEntity entity, CancellationToken cancellationToken = default)
        => _dbContext.SystemParameters.AddAsync(entity, cancellationToken).AsTask();

    public void Update(SystemParameterEntity entity)
        => _dbContext.SystemParameters.Update(entity);
}
