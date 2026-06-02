using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;

public sealed class SystemParameterDto
{
    public long Id { get; set; }
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public SystemParameterValueType ValueType { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsActive { get; set; }
}
