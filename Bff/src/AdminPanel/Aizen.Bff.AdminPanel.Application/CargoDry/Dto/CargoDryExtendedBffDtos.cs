namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

/// <summary>
/// Prior-period comparison for CargoDry dashboard KPI change badges.
/// All percent values are nullable — null means no prior data to compare.
/// </summary>
[DocumentationInfo("CargoDry stats comparison BFF DTO",
    "30-day window vs prior 30-day window percent changes for dashboard badge rendering.")]
public sealed class CargoDryStatsComparisonBffDto
{
    public double? ActiveKitsChangePercent   { get; init; }
    public double? TotalKitsChangePercent    { get; init; }
    public double? TodayActivationsChangePct { get; init; }
    public double? RenewalRateDelta          { get; init; }
    public string  CurrentPeriodStart        { get; init; } = default!;
    public string  PriorPeriodStart          { get; init; } = default!;
}

/// <summary>
/// A warehouse / dispatch location option for the Generate Batch modal.
/// MVP: static list. Post-MVP: backed by warehouse registry entity.
/// </summary>
[DocumentationInfo("CargoDry warehouse option BFF DTO",
    "Warehouse picker option for batch generation. Code is stored on CargoDryBatchEntity.WarehouseCode.")]
public sealed class CargoDryWarehouseOptionBffDto
{
    public string  Code         { get; init; } = default!;
    public string  Name         { get; init; } = default!;
    public string? LocationName { get; init; }
    public string? CountryCode  { get; init; }
    public bool    IsActive     { get; init; }
}

/// <summary>Byte stream result for CSV download endpoints.</summary>
public sealed class CargoDryExportBffResponse
{
    public byte[] Bytes       { get; init; } = [];
    public string ContentType { get; init; } = "text/csv";
    public string FileName    { get; init; } = "export.csv";
}
