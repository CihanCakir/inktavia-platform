using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Renewal Preparation entity",
    "Stateful workflow record for the admin-driven kit renewal funnel. " +
    "One open preparation per kit at a time (enforced by repository query). " +
    "Tracks renewal pricing, invoice reference, notification dispatch intent, and completion. " +
    "Notification delivery is delegated to the Notification module via message bus; " +
    "CargoDry stores only the correlation reference and dispatch status. " +
    "Renewal completion calls the existing RenewKitCommand — the preparation does NOT " +
    "directly mutate the kit entity. " +
    "Phase 11 (July 2026).")]
public sealed class CargoDryRenewalPreparationEntity : AizenEntityWithAudit
{
    // ── Identity ───────────────────────────────────────────────────────────────
    public string RenewalCode { get; private set; } = default!;

    // ── Kit context ────────────────────────────────────────────────────────────
    public long    KitId       { get; private set; }
    public string  KitCode     { get; private set; } = default!;
    public string  ProductCode { get; private set; } = default!;

    // ── Owner / vessel / provider context ─────────────────────────────────────
    public long?  OwnerUserId      { get; private set; }
    public long?  VesselId         { get; private set; }
    public long?  ProviderProfileId { get; private set; }

    // ── Renewal parameters ─────────────────────────────────────────────────────
    public DateTimeOffset  CurrentExpiresAtUtc   { get; private set; }
    public int             RequestedRenewalMonths { get; private set; }
    public DateTimeOffset? NewExpiresAtUtc        { get; private set; }
    public decimal         RenewalPrice           { get; private set; }
    public string          CurrencyCode           { get; private set; } = default!;

    // ── Status ─────────────────────────────────────────────────────────────────
    public CargoDryRenewalPreparationStatus Status { get; private set; }

    // ── Payment / invoice references (cross-module Id-only, no EF FK) ─────────
    public long?   InvoiceId              { get; private set; }
    public long?   PaymentTransactionId   { get; private set; }
    public string? ManualPaymentReference { get; private set; }

    // ── Notification tracking ──────────────────────────────────────────────────
    public string?                         NotificationCorrelationId       { get; private set; }
    public CargoDryRenewalNotificationStatus NotificationStatus            { get; private set; }
    public string?                         NotificationChannels            { get; private set; } // JSON array: ["Email","Sms"]
    public string?                         LastNotificationTemplateCode    { get; private set; }
    public string?                         LastNotificationLanguageCode    { get; private set; }
    public DateTimeOffset?                 NotificationPreparedAtUtc       { get; private set; }
    public long?                           NotificationPreparedByUserId    { get; private set; }
    public DateTimeOffset?                 NotificationDispatchedAtUtc     { get; private set; }
    public long?                           NotificationDispatchedByUserId  { get; private set; }
    public string?                         NotificationFailureReason       { get; private set; }

    // ── Lifecycle audit ────────────────────────────────────────────────────────
    public long?           PreparedByUserId  { get; private set; }
    public DateTimeOffset  PreparedAtUtc     { get; private set; }
    public long?           CompletedByUserId { get; private set; }
    public DateTimeOffset? CompletedAtUtc    { get; private set; }
    public long?           CancelledByUserId { get; private set; }
    public DateTimeOffset? CancelledAtUtc    { get; private set; }
    public string?         CancellationReason { get; private set; }
    public string?         Note              { get; private set; }

    private CargoDryRenewalPreparationEntity() { }

    // ── Factory ────────────────────────────────────────────────────────────────
    public static CargoDryRenewalPreparationEntity Create(
        string         renewalCode,
        long           kitId,
        string         kitCode,
        string         productCode,
        long?          ownerUserId,
        long?          vesselId,
        long?          providerProfileId,
        DateTimeOffset currentExpiresAtUtc,
        int            requestedRenewalMonths,
        decimal        renewalPrice,
        string         currencyCode,
        long?          preparedByUserId,
        string?        note = null)
        => new()
        {
            RenewalCode            = renewalCode,
            KitId                  = kitId,
            KitCode                = kitCode,
            ProductCode            = productCode,
            OwnerUserId            = ownerUserId,
            VesselId               = vesselId,
            ProviderProfileId      = providerProfileId,
            CurrentExpiresAtUtc   = currentExpiresAtUtc,
            RequestedRenewalMonths = requestedRenewalMonths,
            RenewalPrice           = renewalPrice,
            CurrencyCode           = currencyCode.ToUpperInvariant(),
            Status                 = CargoDryRenewalPreparationStatus.Prepared,
            NotificationStatus     = CargoDryRenewalNotificationStatus.None,
            PreparedByUserId       = preparedByUserId,
            PreparedAtUtc          = DateTimeOffset.UtcNow,
            Note                   = note,
            IsActive               = true,
        };

    // ── State transitions ──────────────────────────────────────────────────────

    public void SetNewExpiryEstimate(DateTimeOffset newExpiresAt)
        => NewExpiresAtUtc = newExpiresAt;

    public void MarkInvoicePrepared(long invoiceId)
    {
        InvoiceId = invoiceId;
        Status    = CargoDryRenewalPreparationStatus.InvoicePrepared;
    }

    public void MarkNotificationPrepared(
        string  templateCode,
        string  languageCode,
        string  channelsJson,
        long    preparedByUserId)
    {
        LastNotificationTemplateCode   = templateCode;
        LastNotificationLanguageCode   = languageCode;
        NotificationChannels           = channelsJson;
        NotificationStatus             = CargoDryRenewalNotificationStatus.Prepared;
        NotificationPreparedAtUtc      = DateTimeOffset.UtcNow;
        NotificationPreparedByUserId   = preparedByUserId;
        Status                         = CargoDryRenewalPreparationStatus.NotificationPrepared;
    }

    public void MarkNotificationQueued(
        string correlationId,
        string channelsJson,
        string templateCode,
        string languageCode,
        long   dispatchedByUserId)
    {
        NotificationCorrelationId      = correlationId;
        NotificationChannels           = channelsJson;
        LastNotificationTemplateCode   = templateCode;
        LastNotificationLanguageCode   = languageCode;
        NotificationStatus             = CargoDryRenewalNotificationStatus.Queued;
        NotificationDispatchedAtUtc    = DateTimeOffset.UtcNow;
        NotificationDispatchedByUserId = dispatchedByUserId;
        Status                         = CargoDryRenewalPreparationStatus.NotificationQueued;
    }

    public void MarkNotificationFailed(string reason)
    {
        NotificationStatus        = CargoDryRenewalNotificationStatus.Failed;
        NotificationFailureReason = reason;
        // Status stays at current — notification failure does NOT cancel or fail the preparation
    }

    public void Complete(long completedByUserId, string? manualPaymentReference = null, string? note = null)
    {
        if (Status == CargoDryRenewalPreparationStatus.Completed)
            return; // idempotent

        if (Status == CargoDryRenewalPreparationStatus.Cancelled)
            throw new InvalidOperationException($"Renewal preparation {Id} ({RenewalCode}) is already cancelled.");

        ManualPaymentReference = manualPaymentReference;
        Status                 = CargoDryRenewalPreparationStatus.Completed;
        CompletedByUserId      = completedByUserId;
        CompletedAtUtc         = DateTimeOffset.UtcNow;
        if (note is not null) Note = note;
    }

    public void Cancel(long cancelledByUserId, string cancellationReason, string? note = null)
    {
        if (Status == CargoDryRenewalPreparationStatus.Completed)
            throw new InvalidOperationException($"Renewal preparation {Id} ({RenewalCode}) is already completed.");

        if (Status == CargoDryRenewalPreparationStatus.Cancelled)
            return; // idempotent

        Status             = CargoDryRenewalPreparationStatus.Cancelled;
        CancelledByUserId  = cancelledByUserId;
        CancelledAtUtc     = DateTimeOffset.UtcNow;
        CancellationReason = cancellationReason;
        if (note is not null) Note = note;
    }

    public void Fail(string reason)
    {
        Status = CargoDryRenewalPreparationStatus.Failed;
        Note   = reason;
    }

    // ── Computed helpers ───────────────────────────────────────────────────────
    public bool IsTerminal
        => Status is CargoDryRenewalPreparationStatus.Completed
                  or CargoDryRenewalPreparationStatus.Cancelled
                  or CargoDryRenewalPreparationStatus.Failed;

    public bool CanPrepareInvoice
        => !IsTerminal && InvoiceId is null;

    public bool CanDispatchNotification
        => !IsTerminal && OwnerUserId.HasValue;

    public bool CanComplete
        => !IsTerminal && (InvoiceId.HasValue || ManualPaymentReference is not null);
}
