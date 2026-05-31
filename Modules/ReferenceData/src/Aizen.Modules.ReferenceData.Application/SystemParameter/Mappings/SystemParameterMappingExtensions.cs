using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Entities.SystemParameter;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Mappings;

public static class SystemParameterMappingExtensions
{
    public static SystemParameterDto ToDto(this SystemParameterEntity entity) => new()
    {
        Id = entity.Id,
        Key = entity.Key,
        Value = entity.Value,
        ValueType = entity.ValueType,
        Description = entity.Description,
        IsEncrypted = entity.IsEncrypted,
        IsActive = entity.IsActive
    };
}
