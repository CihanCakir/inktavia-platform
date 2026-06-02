using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class CreateSystemParameterCommand : AizenCommand<SystemParameterDto>
{
    public string Key { get; }
    public string Value { get; }
    public SystemParameterValueType ValueType { get; }
    public string? Description { get; }
    public bool IsEncrypted { get; }

    public CreateSystemParameterCommand(string key, string value, SystemParameterValueType valueType, string? description, bool isEncrypted)
    {
        Key = key;
        Value = value;
        ValueType = valueType;
        Description = description;
        IsEncrypted = isEncrypted;
    }
}
