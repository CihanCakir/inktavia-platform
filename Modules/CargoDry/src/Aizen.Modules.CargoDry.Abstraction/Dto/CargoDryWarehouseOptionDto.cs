namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// A CargoDry dispatch warehouse/location option for batch generation.
/// MVP: sourced from static configuration. Post-MVP: backed by a warehouse registry entity.
/// </summary>
public sealed class CargoDryWarehouseOptionDto
{
    public string  Code          { get; init; } = default!;
    public string  Name          { get; init; } = default!;
    public string? LocationName  { get; init; }
    public string? CountryCode   { get; init; }
    public bool    IsActive      { get; init; }
}
