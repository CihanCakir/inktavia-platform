namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public class CargoDryKitUsageReportDto
{
    public int    TotalKits             { get; set; }
    public int    ActivatedKits         { get; set; }
    public int    ExpiredKits           { get; set; }
    public int    RevokedKits           { get; set; }
    public int    RenewedKits           { get; set; }
    public double AverageEfficiencyPct  { get; set; }
    public double RenewalRatePct        { get; set; }
    public double AvgActiveDaysAtExpiry { get; set; }

    public List<CargoDryProductUsageRow>        ByProduct        { get; set; } = [];
    public List<CargoDryBatchUsageRow>           ByBatch          { get; set; } = [];
    public List<CargoDryDailyActivationPoint>    DailyActivations { get; set; } = [];

    public DateTimeOffset  GeneratedAt { get; set; }
    public DateTimeOffset? DateFrom    { get; set; }
    public DateTimeOffset? DateTo      { get; set; }
}

public class CargoDryProductUsageRow
{
    public string ProductCode      { get; set; } = "";
    public string ProductName      { get; set; } = "";
    public int    TotalKits        { get; set; }
    public int    ActivatedKits    { get; set; }
    public int    ExpiredKits      { get; set; }
    public int    RenewedKits      { get; set; }
    public double AvgEfficiencyPct { get; set; }
}

public class CargoDryBatchUsageRow
{
    public string         BatchCode     { get; set; } = "";
    public string         ProductCode   { get; set; } = "";
    public int            TotalKits     { get; set; }
    public int            ActivatedKits { get; set; }
    public int            ExpiredKits   { get; set; }
    public DateTimeOffset CreatedAt     { get; set; }
}

public class CargoDryDailyActivationPoint
{
    public DateOnly Date        { get; set; }
    public int      Activations { get; set; }
    public int      Renewals    { get; set; }
}
