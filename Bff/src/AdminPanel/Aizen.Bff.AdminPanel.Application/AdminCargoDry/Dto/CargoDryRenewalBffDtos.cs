namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;

// ── Phase 11: CargoDry Renewal Billing & Notification Orchestration ──────────

/// <summary>
/// Single renewal candidate as exposed to Admin Web.
/// Mirrors CargoDryRenewalCandidateDto — no BFF-level enrichment at MVP.
/// Phase 11 (July 2026).
/// </summary>
public sealed class CargoDryRenewalCandidateBffDto
{
    public long    KitId                           { get; init; }
    public string  KitCode                         { get; init; } = default!;
    public string  SerialNumber                    { get; init; } = default!;
    public string  ProductCode                     { get; init; } = default!;
    public string? ProductName                     { get; init; }
    public string  BatchCode                       { get; init; } = default!;
    public string  Status                          { get; init; } = default!;

    public long?   OwnerUserId                     { get; init; }
    public string? OwnerDisplayName                { get; init; }
    public long?   VesselId                        { get; init; }
    public string? VesselName                      { get; init; }
    public long?   ProviderProfileId               { get; init; }

    public DateTimeOffset? ExpiresAtUtc             { get; init; }
    public int             DaysUntilExpiry          { get; init; }
    public int             RecommendedRenewalMonths { get; init; }
    public decimal?        RenewalPrice             { get; init; }
    public string?         CurrencyCode             { get; init; }

    public bool          CanPrepareRenewal               { get; init; }
    public List<string>  BlockingReasons                 { get; init; } = new();
    public List<string>  Warnings                        { get; init; } = new();

    public DateTimeOffset? LastRenewalAtUtc               { get; init; }
    public DateTimeOffset? LastNotificationPreparedAtUtc  { get; init; }
    public DateTimeOffset? LastNotificationDispatchedAtUtc { get; init; }
    public string?         LastNotificationStatus          { get; init; }
    public long?           OpenRenewalPreparationId        { get; init; }
}

/// <summary>
/// Full renewal preparation record as exposed to Admin Web.
/// Mirrors CargoDryRenewalPreparationDto — no BFF-level enrichment at MVP.
/// Phase 11 (July 2026).
/// </summary>
public sealed class CargoDryRenewalPreparationBffDto
{
    public long   Id          { get; init; }
    public string RenewalCode { get; init; } = default!;

    // Kit context
    public long   KitId       { get; init; }
    public string KitCode     { get; init; } = default!;
    public string ProductCode { get; init; } = default!;
    public string? ProductName { get; init; }

    // Owner / vessel / provider
    public long?   OwnerUserId       { get; init; }
    public string? OwnerDisplayName  { get; init; }
    public long?   VesselId          { get; init; }
    public string? VesselName        { get; init; }
    public long?   ProviderProfileId { get; init; }

    // Renewal parameters
    public DateTimeOffset  CurrentExpiresAtUtc    { get; init; }
    public int             RequestedRenewalMonths { get; init; }
    public DateTimeOffset? NewExpiresAtUtc        { get; init; }
    public decimal         RenewalPrice           { get; init; }
    public string          CurrencyCode           { get; init; } = default!;

    // Status (string representation of enum)
    public string Status { get; init; } = default!;

    // Payment / invoice
    public long?   InvoiceId              { get; init; }
    public long?   PaymentTransactionId   { get; init; }
    public string? ManualPaymentReference { get; init; }

    // Notification
    public string  NotificationStatus           { get; init; } = default!;
    public string? NotificationCorrelationId    { get; init; }
    public string? NotificationChannels         { get; init; }
    public string? LastNotificationTemplateCode { get; init; }
    public string? LastNotificationLanguageCode { get; init; }
    public DateTimeOffset? NotificationPreparedAtUtc    { get; init; }
    public DateTimeOffset? NotificationDispatchedAtUtc  { get; init; }
    public string? NotificationFailureReason    { get; init; }

    // Lifecycle audit
    public long?   PreparedByUserId   { get; init; }
    public DateTimeOffset  PreparedAtUtc      { get; init; }
    public long?   CompletedByUserId  { get; init; }
    public DateTimeOffset? CompletedAtUtc     { get; init; }
    public long?   CancelledByUserId  { get; init; }
    public DateTimeOffset? CancelledAtUtc     { get; init; }
    public string? CancellationReason { get; init; }
    public string? Note               { get; init; }

    // Computed helpers
    public bool         CanPrepareInvoice       { get; init; }
    public bool         CanDispatchNotification { get; init; }
    public bool         CanComplete             { get; init; }
    public List<string> BlockingReasons         { get; init; } = new();
    public List<string> Warnings                { get; init; } = new();
}

/// <summary>
/// Paged list of renewal candidate kits returned by GET /renewals/candidates.
/// Phase 11 (July 2026).
/// </summary>
public sealed class CargoDryRenewalCandidatesPagedBffResponse
{
    public List<CargoDryRenewalCandidateBffDto> Items    { get; init; } = new();
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// Paged list of renewal preparations returned by GET /renewals.
/// Phase 11 (July 2026).
/// </summary>
public sealed class CargoDryRenewalPreparationsPagedBffResponse
{
    public List<CargoDryRenewalPreparationBffDto> Items    { get; init; } = new();
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
    public int Pages    { get; init; }
}
