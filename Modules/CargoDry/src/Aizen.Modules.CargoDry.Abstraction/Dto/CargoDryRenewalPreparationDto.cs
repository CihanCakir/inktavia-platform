using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryRenewalPreparationDto
{
    public long                                  Id                            { get; init; }
    public string                                RenewalCode                   { get; init; } = default!;

    // Kit context
    public long                                  KitId                         { get; init; }
    public string                                KitCode                       { get; init; } = default!;
    public string                                ProductCode                   { get; init; } = default!;
    public string?                               ProductName                   { get; init; }

    // Owner / vessel / provider
    public long?                                 OwnerUserId                   { get; init; }
    public string?                               OwnerDisplayName              { get; init; }
    public long?                                 VesselId                      { get; init; }
    public string?                               VesselName                    { get; init; }
    public long?                                 ProviderProfileId             { get; init; }

    // Renewal parameters
    public DateTimeOffset                        CurrentExpiresAtUtc           { get; init; }
    public int                                   RequestedRenewalMonths        { get; init; }
    public DateTimeOffset?                       NewExpiresAtUtc               { get; init; }
    public decimal                               RenewalPrice                  { get; init; }
    public string                                CurrencyCode                  { get; init; } = default!;

    // Status
    public CargoDryRenewalPreparationStatus      Status                        { get; init; }

    // Payment / invoice
    public long?                                 InvoiceId                     { get; init; }
    public long?                                 PaymentTransactionId          { get; init; }
    public string?                               ManualPaymentReference        { get; init; }

    // Notification
    public CargoDryRenewalNotificationStatus     NotificationStatus            { get; init; }
    public string?                               NotificationCorrelationId     { get; init; }
    public string?                               NotificationChannels          { get; init; }
    public string?                               LastNotificationTemplateCode  { get; init; }
    public string?                               LastNotificationLanguageCode  { get; init; }
    public DateTimeOffset?                       NotificationPreparedAtUtc     { get; init; }
    public DateTimeOffset?                       NotificationDispatchedAtUtc   { get; init; }
    public string?                               NotificationFailureReason     { get; init; }

    // Lifecycle audit
    public long?           PreparedByUserId   { get; init; }
    public DateTimeOffset  PreparedAtUtc      { get; init; }
    public long?           CompletedByUserId  { get; init; }
    public DateTimeOffset? CompletedAtUtc     { get; init; }
    public long?           CancelledByUserId  { get; init; }
    public DateTimeOffset? CancelledAtUtc     { get; init; }
    public string?         CancellationReason { get; init; }
    public string?         Note               { get; init; }

    // Computed helpers
    public bool CanPrepareInvoice       { get; init; }
    public bool CanDispatchNotification { get; init; }
    public bool CanComplete             { get; init; }
    public List<string> BlockingReasons { get; init; } = new();
    public List<string> Warnings        { get; init; } = new();
}
