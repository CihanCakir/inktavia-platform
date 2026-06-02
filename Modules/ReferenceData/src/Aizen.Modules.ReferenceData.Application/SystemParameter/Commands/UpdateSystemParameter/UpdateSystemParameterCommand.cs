using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class UpdateSystemParameterCommand : AizenCommand<SystemParameterDto>
{
    public string Key { get; }
    public string Value { get; }
    public string? Description { get; }
    public bool IsActive { get; }

    public UpdateSystemParameterCommand(string key, string value, string? description, bool isActive)
    {
        Key = key;
        Value = value;
        Description = description;
        IsActive = isActive;
    }
}
