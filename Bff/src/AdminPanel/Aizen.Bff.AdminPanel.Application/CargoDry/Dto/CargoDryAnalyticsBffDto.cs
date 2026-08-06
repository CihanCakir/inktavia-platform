namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

// ── Raw DTO (matches backend JSON shape for Refit deserialization) ─────────────

/// <summary>
/// Mirrors the backend CargoDryAnalyticsDto JSON exactly.
/// StatusDistribution and EfficiencyBuckets are dictionaries in the backend;
/// they are mapped to lists in the BFF DTO with colour added.
/// </summary>
public class CargoDryAnalyticsRawBffDto
{
    public Dictionary<string, int>             StatusDistribution { get; set; } = [];
    public List<CargoDryAnalyticsDailyRawDto>  DailyActivations   { get; set; } = [];
    public Dictionary<string, int>             EfficiencyBuckets  { get; set; } = [];
    public List<CargoDryProductMixRawDto>      ProductMix         { get; set; } = [];
    public int            TotalKits          { get; set; }
    public int            ActiveKits         { get; set; }
    public double         AvgEfficiencyPct   { get; set; }
    public double         RenewalRatePct     { get; set; }
    public int            ExpiringNext30Days { get; set; }
    public DateTimeOffset ComputedAt         { get; set; }
}

public class CargoDryAnalyticsDailyRawDto
{
    public string Date        { get; set; } = "";
    public int    Activations { get; set; }
    public int    Renewals    { get; set; }
}

public class CargoDryProductMixRawDto
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int    ActiveKits  { get; set; }
    public int    TotalKits   { get; set; }
}

// ── Chart-ready DTO (returned to frontend) ────────────────────────────────────

[DocumentationInfo("CargoDry analytics BFF DTO", "Pre-shaped for Recharts. All series values are chart-ready. Colors assigned by BFF for consistent palette.")]
public class CargoDryAnalyticsBffDto
{
    public List<CargoDryStatusSliceDto>        StatusDistribution { get; set; } = [];
    public List<CargoDryDailyActivationBffDto> DailyActivations   { get; set; } = [];
    public List<CargoDryEfficiencyBucketDto>   EfficiencyBuckets  { get; set; } = [];
    public List<CargoDryProductMixBffDto>      ProductMix         { get; set; } = [];

    public int            TotalKits          { get; set; }
    public int            ActiveKits         { get; set; }
    public double         AvgEfficiencyPct   { get; set; }
    public double         RenewalRatePct     { get; set; }
    public int            ExpiringNext30Days { get; set; }
    public DateTimeOffset ComputedAt         { get; set; }
}

public class CargoDryStatusSliceDto
{
    public string Name  { get; set; } = "";  // "Available", "Activated", "Expired" …
    public int    Value { get; set; }
    public string Color { get; set; } = "";  // HEX colour assigned by BFF
}

public class CargoDryEfficiencyBucketDto
{
    public string Bucket { get; set; } = ""; // "0–25%", "25–50%", "50–75%", "75–100%"
    public int    Count  { get; set; }
}

public class CargoDryProductMixBffDto
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int    ActiveKits  { get; set; }
    public int    TotalKits   { get; set; }
}
