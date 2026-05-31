using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Domain.Entities.Measurement;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface IMeasurementUnitRepository
{
    Task<MeasurementUnitEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<MeasurementUnitEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeasurementUnitEntity>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeasurementUnitEntity>> GetByTypeAsync(MeasurementUnitType unitType, bool onlyActive, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(MeasurementUnitEntity entity, CancellationToken cancellationToken = default);
    void Update(MeasurementUnitEntity entity);
}
