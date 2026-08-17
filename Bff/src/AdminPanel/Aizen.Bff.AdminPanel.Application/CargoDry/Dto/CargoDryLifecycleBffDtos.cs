namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

// ── Phase 9: Kit Lifecycle History & Operational Alerts ──────────────────────

/// <summary>
/// Single lifecycle event as exposed by the BFF to Admin Web.
/// Mirrors CargoDryKitLifecycleEventDto — no enrichment added at BFF level (MVP).
/// </summary>
public sealed class CargoDryKitLifecycleEventBffDto
{
    public long    Id            { get; init; }
    public long    KitId         { get; init; }
    public string  KitCode       { get; init; } = default!;
    public string? SerialNumber  { get; init; }
    public string? BatchCode     { get; init; }
    public string? ProductCode   { get; init; }

    public string  EventType      { get; init; } = default!;
    public string? PreviousStatus { get; init; }
    public string? NewStatus      { get; init; }

    public long?   ActorUserId   { get; init; }
    public string? ActorType     { get; init; }

    public string? Reason        { get; init; }
    public string? Note          { get; init; }
    public long?   ReferenceId   { get; init; }
    public string? ReferenceType { get; init; }
    public string? MetadataJson  { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }
}

/// <summary>
/// Full lifecycle history for a single kit (ordered by OccurredAtUtc desc from module).
/// </summary>
public sealed class CargoDryKitLifecycleHistoryBffResponse
{
    public long   KitId   { get; init; }
    public string KitCode { get; init; } = default!;
    public IReadOnlyList<CargoDryKitLifecycleEventBffDto> Events { get; init; }
        = Array.Empty<CargoDryKitLifecycleEventBffDto>();
}

/// <summary>
/// Paginated cross-kit lifecycle events for the global Lifecycle Events admin page.
/// </summary>
public sealed class CargoDryKitLifecycleEventsPagedBffResponse
{
    public IReadOnlyList<CargoDryKitLifecycleEventBffDto> Items { get; init; }
        = Array.Empty<CargoDryKitLifecycleEventBffDto>();
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// Single operational alert item as exposed to Admin Web.
/// </summary>
public sealed class CargoDryOperationalAlertBffDto
{
    public long    KitId              { get; init; }
    public string  KitCode            { get; init; } = default!;
    public string? SerialNumber       { get; init; }
    public string? BatchCode          { get; init; }
    public string? ProductCode        { get; init; }
    public string? ProductName        { get; init; }
    public long?   OwnerUserId        { get; init; }
    public string? OwnerDisplayName   { get; init; }
    public long?   VesselId           { get; init; }
    public string? VesselName         { get; init; }
    public long?   ProviderProfileId  { get; init; }

    /// <summary>ExpiringSoon | Expired | Revoked | CommercialReviewRequired | RenewalDue</summary>
    public string  AlertType       { get; init; } = default!;
    /// <summary>Info | Warning | Critical</summary>
    public string  Severity        { get; init; } = default!;
    public string  Message         { get; init; } = default!;
    public int?    DaysUntilExpiry { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string  KitStatus       { get; init; } = default!;
}

/// <summary>
/// Paginated operational alerts response.
/// </summary>
public sealed class CargoDryOperationalAlertsBffResponse
{
    public IReadOnlyList<CargoDryOperationalAlertBffDto> Items { get; init; }
        = Array.Empty<CargoDryOperationalAlertBffDto>();
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// Rich operational KPI snapshot for the CargoDry admin dashboard.
/// Computed at query time, cached 2 min in module layer.
/// </summary>
public sealed class CargoDryOperationalOverviewBffDto
{
    // Kit lifecycle counts
    public int TotalKits                    { get; init; }
    public int AvailableKits                { get; init; }
    public int ActivatedKits                { get; init; }
    public int ExpiredKits                  { get; init; }
    public int RevokedKits                  { get; init; }
    public int LostKits                     { get; init; }
    public int RenewalDueSoonKits           { get; init; }
    public int CommercialReviewRequiredKits { get; init; }
    public int ProviderHeldKits             { get; init; }
    public int WarehouseStockKits           { get; init; }

    // Batch counts
    public int TotalBatches              { get; init; }
    public int ActiveBatches             { get; init; }
    public int ProviderAllocatedBatches  { get; init; }

    // Activity indicators
    public int RecentLifecycleEventCount { get; init; }
    public int OpenOperationalAlertCount { get; init; }

    public DateTimeOffset ComputedAtUtc { get; init; }
}
