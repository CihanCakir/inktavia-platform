using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// CargoDry supply v2 — a single kit in the provider's own inventory, as shown in the provider-portal
/// kit picker. Backs GET /api/v1/cargodry/provider/kits (and its MarineProvider BFF passthrough).
/// </summary>
public sealed class CargoDryProviderKitDto
{
    public long              KitId        { get; init; }
    public string            KitCode      { get; init; } = default!;
    public string?           SerialNumber { get; init; }
    public CargoDryKitStatus Status       { get; init; }
    public string            StatusName   { get; init; } = default!;
    public string?           ProductCode  { get; init; }
    public string?           ProductName  { get; init; }
}
