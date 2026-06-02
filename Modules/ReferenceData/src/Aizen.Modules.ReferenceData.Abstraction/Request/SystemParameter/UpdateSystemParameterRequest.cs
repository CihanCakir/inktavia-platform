namespace Aizen.Modules.ReferenceData.Abstraction.Request.SystemParameter;

public sealed class UpdateSystemParameterRequest
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
