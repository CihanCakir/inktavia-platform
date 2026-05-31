using Aizen.Core.Domain;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Domain.Entities.SystemParameter;

public sealed class SystemParameterEntity : AizenEntityWithAudit
{
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public SystemParameterValueType ValueType { get; private set; }
    public string? Description { get; private set; }
    public bool IsEncrypted { get; private set; }

    private SystemParameterEntity() { }

    public static SystemParameterEntity Create(string key, string value, SystemParameterValueType valueType, string? description, bool isEncrypted)
    {
        return new SystemParameterEntity
        {
            Key = key.Trim(),
            Value = value,
            ValueType = valueType,
            Description = description,
            IsEncrypted = isEncrypted,
            IsActive = true
        };
    }

    public void Update(string value, string? description, bool isActive)
    {
        Value = value;
        Description = description;
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
