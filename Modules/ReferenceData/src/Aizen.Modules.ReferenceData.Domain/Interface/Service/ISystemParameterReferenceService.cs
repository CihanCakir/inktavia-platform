using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface ISystemParameterReferenceService
{
    Task<SystemParameterDto> CreateAsync(string key, string value, SystemParameterValueType valueType, string? description, bool isEncrypted, CancellationToken cancellationToken = default);
    Task<SystemParameterDto> UpdateAsync(string key, string value, string? description, bool isActive, CancellationToken cancellationToken = default);
    Task ActivateAsync(string key, CancellationToken cancellationToken = default);
    Task DeactivateAsync(string key, CancellationToken cancellationToken = default);
    Task<SystemParameterDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemParameterDto>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemParameterDto>> GetByPrefixAsync(string prefix, bool onlyActive, CancellationToken cancellationToken = default);
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);
    Task<bool?> GetBooleanAsync(string key, CancellationToken cancellationToken = default);
    Task<int?> GetIntAsync(string key, CancellationToken cancellationToken = default);
    Task<decimal?> GetDecimalAsync(string key, CancellationToken cancellationToken = default);
}
