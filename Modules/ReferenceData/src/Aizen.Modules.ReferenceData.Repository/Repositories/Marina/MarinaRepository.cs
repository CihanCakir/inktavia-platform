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
}
