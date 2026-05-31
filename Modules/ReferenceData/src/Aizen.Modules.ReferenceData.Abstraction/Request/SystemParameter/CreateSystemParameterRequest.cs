using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Request.SystemParameter;

public sealed class CreateSystemParameterRequest
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public SystemParameterValueType ValueType { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
}
