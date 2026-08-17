namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryStatsDto
{
    public int TotalKits              { get; init; }
    public int AvailableKits          { get; init; }
    public int ActiveKits             { get; init; }
    public int ExpiringKits           { get; init; }
    public int ExpiredKits            { get; init; }
    public int RevokedKits            { get; init; }
    public int TodayActivations       { get; init; }
    public int TotalBatches           { get; init; }
    public double RenewalRatePercent  { get; init; }
}
