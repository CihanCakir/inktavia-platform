using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Abstraction.Request.Measurement;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface IMeasurementReferenceService
{
    Task<MeasurementUnitDto> CreateAsync(CreateMeasurementUnitRequest request, CancellationToken cancellationToken = default);
    Task<MeasurementUnitDto> UpdateAsync(long id, string name, string symbol, decimal? conversionFactorToBase, string? baseUnitCode, bool isActive, CancellationToken cancellationToken = default);
    Task ActivateAsync(long id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeasurementUnitDto>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<MeasurementUnitDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    /// <summary>R3 — resolves a measurement unit by its (normalized) code; null if none.</summary>
    Task<MeasurementUnitDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeasurementUnitDto>> GetByTypeAsync(MeasurementUnitType unitType, bool onlyActive, CancellationToken cancellationToken = default);
}
