namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

[DocumentationInfo("CargoDry usage report BFF DTOs", "Pre-shaped for admin panel kit usage report page. 1:1 passthrough from backend report DTO.")]
public class CargoDryKitUsageReportBffDto
{
    public int    TotalKits             { get; set; }
    public int    ActivatedKits         { get; set; }
    public int    ExpiredKits           { get; set; }
    public int    RevokedKits           { get; set; }
    public int    RenewedKits           { get; set; }
    public double AverageEfficiencyPct  { get; set; }
    public double RenewalRatePct        { get; set; }
    public double AvgActiveDaysAtExpiry { get; set; }
    public List<CargoDryProductUsageRowBffDto> ByProduct        { get; set; } = [];
    public List<CargoDryBatchUsageRowBffDto>   ByBatch          { get; set; } = [];
    public List<CargoDryDailyActivationBffDto> DailyActivations { get; set; } = [];
    public DateTimeOffset GeneratedAt { get; set; }
}

public class CargoDryProductUsageRowBffDto
{
    public string ProductCode      { get; set; } = "";
    public string ProductName      { get; set; } = "";
    public int    TotalKits        { get; set; }
    public int    ActivatedKits    { get; set; }
    public int    ExpiredKits      { get; set; }
    public int    RenewedKits      { get; set; }
    public double AvgEfficiencyPct { get; set; }
}

public class CargoDryBatchUsageRowBffDto
{
    public string BatchCode     { get; set; } = "";
    public string ProductCode   { get; set; } = "";
    public int    TotalKits     { get; set; }
    public int    ActivatedKits { get; set; }
    public int    ExpiredKits   { get; set; }
    public string CreatedAt     { get; set; } = "";
}

public class CargoDryDailyActivationBffDto
{
    public string Date        { get; set; } = ""; // ISO date "2025-01-15"
    public int    Activations { get; set; }
    public int    Renewals    { get; set; }
}
