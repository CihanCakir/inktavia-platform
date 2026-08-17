namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Severity levels for admin operational alerts.
/// </summary>
public enum CargoDryAlertSeverity
{
    Info     = 0,
    Warning  = 1,
    Critical = 2,
}

/// <summary>
/// Represents a single kit-level operational alert derived from query logic (no persisted alert table).
/// </summary>
public sealed class CargoDryOperationalAlertDto
{
    public long    KitId           { get; init; }
    public string  KitCode         { get; init; } = default!;
    public string? SerialNumber    { get; init; }
    public string? BatchCode       { get; init; }
    public string? ProductCode     { get; init; }
    public string? ProductName     { get; init; }
    public long?   OwnerUserId     { get; init; }
    public string? OwnerDisplayName{ get; init; }
    public long?   VesselId        { get; init; }
    public string? VesselName      { get; init; }
    public long?   ProviderProfileId { get; init; }

    public string             AlertType    { get; init; } = default!; // ExpiringSoon | Expired | Revoked | CommercialReviewRequired | RenewalDue
    public CargoDryAlertSeverity Severity  { get; init; }
    public string             Message      { get; init; } = default!;
    public int?               DaysUntilExpiry { get; init; }
    public DateTimeOffset?    ExpiresAt    { get; init; }
    public string             KitStatus    { get; init; } = default!;
}

public sealed class CargoDryOperationalAlertsResponse
{
    public IReadOnlyList<CargoDryOperationalAlertDto> Items { get; init; }
        = Array.Empty<CargoDryOperationalAlertDto>();
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}
