using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Entities.Measurement;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Mappings;

public static class MeasurementMappingExtensions
{
    public static MeasurementUnitDto ToDto(this MeasurementUnitEntity entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Symbol = entity.Symbol,
        UnitType = entity.UnitType,
        ConversionFactorToBase = entity.ConversionFactorToBase,
        BaseUnitCode = entity.BaseUnitCode,
        IsActive = entity.IsActive
    };
}
