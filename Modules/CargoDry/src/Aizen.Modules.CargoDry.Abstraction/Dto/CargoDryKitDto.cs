using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryKitDto
{
    public long              Id                { get; init; }
    public string            SerialNumber      { get; init; } = default!;
    public string            KitCode           { get; init; } = default!;
    public string            ProductCode       { get; init; } = default!;
    public string            ProductName       { get; init; } = default!;
    public string            BatchCode         { get; init; } = default!;
    public CargoDryKitStatus Status            { get; init; }
    public long?             OwnerUserId       { get; init; }
    public string?           OwnerDisplayName  { get; init; }
    public long?             VesselId          { get; init; }
    public string?           VesselName        { get; init; }
    public DateTimeOffset?   ActivatedAt       { get; init; }
    public DateTimeOffset?   ExpiresAt         { get; init; }
    public double            EfficiencyPercent { get; init; }
    public int               DaysUntilExpiry   { get; init; }
    public int               RenewalCount      { get; init; }
    public DateTimeOffset    ManufacturedAt    { get; init; }
}
