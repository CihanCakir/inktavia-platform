using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Domain.Entities.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Repositories.Measurement;

public sealed class MeasurementUnitRepository : IMeasurementUnitRepository
{
    private readonly ReferenceDataDbContext _dbContext;

    public MeasurementUnitRepository(ReferenceDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<MeasurementUnitEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _dbContext.MeasurementUnits.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<MeasurementUnitEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.MeasurementUnits.FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyList<MeasurementUnitEntity>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MeasurementUnits.AsNoTracking();
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.UnitType).ThenBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MeasurementUnitEntity>> GetByTypeAsync(MeasurementUnitType unitType, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MeasurementUnits.AsNoTracking().Where(x => x.UnitType == unitType);
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.MeasurementUnits.AnyAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public Task AddAsync(MeasurementUnitEntity entity, CancellationToken cancellationToken = default)
        => _dbContext.MeasurementUnits.AddAsync(entity, cancellationToken).AsTask();

    public void Update(MeasurementUnitEntity entity)
        => _dbContext.MeasurementUnits.Update(entity);
}
