namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryKitValidationDto
{
    public bool    IsValid          { get; init; }
    public string? InvalidReason    { get; init; }
    public string? ProductName      { get; init; }
    public string? ProductCode      { get; init; }
    public int     ValidityDays     { get; init; }
    public bool    HasSmartDevice   { get; init; }
    public string? ActivationToken  { get; init; }
    public DateTimeOffset? TokenExpiresAt { get; init; }
}
