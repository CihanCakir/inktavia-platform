namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public class CargoDryAnalyticsDto
{
    public Dictionary<string, int>         StatusDistribution { get; set; } = [];
    public List<CargoDryDailyActivationPoint> DailyActivations { get; set; } = [];
    public Dictionary<string, int>         EfficiencyBuckets  { get; set; } = [];
    public List<CargoDryProductMixRow>     ProductMix         { get; set; } = [];

    public int            TotalKits          { get; set; }
    public int            ActiveKits         { get; set; }
    public double         AvgEfficiencyPct   { get; set; }
    public double         RenewalRatePct     { get; set; }
    public int            ExpiringNext30Days { get; set; }
    public DateTimeOffset ComputedAt         { get; set; }
}

public class CargoDryProductMixRow
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int    ActiveKits  { get; set; }
    public int    TotalKits   { get; set; }
}
