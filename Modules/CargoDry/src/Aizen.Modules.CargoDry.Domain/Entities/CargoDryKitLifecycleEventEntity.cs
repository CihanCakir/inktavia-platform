using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

/// <summary>
/// Append-only audit record for every kit lifecycle transition.
/// Stored in PostgreSQL (CargoDry schema, table: kit_lifecycle_events).
/// Do NOT delete or mutate records — only append.
/// Do NOT store sensitive QR activation secrets in MetadataJson.
/// </summary>
[DocumentationInfo("CargoDry Kit Lifecycle Event entity",
    "Append-only SQL audit log for kit state transitions. Written by " +
    "RevokeKitCommandHandler, RenewKitCommandHandler, TransferCargoDryKitCommandHandler, " +
    "and ActivateKitCommandHandler. Queried by lifecycle history and operational alert endpoints. " +
    "Phase 9, July 2026.")]
public sealed class CargoDryKitLifecycleEventEntity : AizenEntityWithAudit
{
    // ── Kit identity snapshot ─────────────────────────────────────────────────
    public long    KitId        { get; private set; }
    public string  KitCode      { get; private set; } = default!;
    public string? SerialNumber { get; private set; }
    public string? BatchCode    { get; private set; }
    public string? ProductCode  { get; private set; }

    // ── Event classification ──────────────────────────────────────────────────
    public CargoDryKitLifecycleEventType EventType      { get; private set; }
    public string?                        PreviousStatus { get; private set; }
    public string?                        NewStatus      { get; private set; }

    // ── Actor ─────────────────────────────────────────────────────────────────
    public long?   ActorUserId { get; private set; }
    public string? ActorType   { get; private set; } // e.g. "Admin", "System", "User"

    // ── Context ───────────────────────────────────────────────────────────────
    public string? Reason        { get; private set; }
    public string? Note          { get; private set; }
    public long?   ReferenceId   { get; private set; }   // e.g. PaymentTransactionId, AgreementId
    public string? ReferenceType { get; private set; }   // e.g. "PaymentTransaction", "Agreement"
    public string? MetadataJson  { get; private set; }   // JSON blob for extra context (no secrets)

    // ── Timestamp ─────────────────────────────────────────────────────────────
    public DateTimeOffset OccurredAtUtc { get; private set; }

    private CargoDryKitLifecycleEventEntity() { }

    public static CargoDryKitLifecycleEventEntity Create(
        long    kitId,
        string  kitCode,
        string? serialNumber,
        string? batchCode,
        string? productCode,
        CargoDryKitLifecycleEventType eventType,
        string? previousStatus,
        string? newStatus,
        long?   actorUserId,
        string? actorType    = null,
        string? reason       = null,
        string? note         = null,
        long?   referenceId  = null,
        string? referenceType = null,
        string? metadataJson = null)
        => new()
        {
            KitId          = kitId,
            KitCode        = kitCode,
            SerialNumber   = serialNumber,
            BatchCode      = batchCode,
            ProductCode    = productCode,
            EventType      = eventType,
            PreviousStatus = previousStatus,
            NewStatus      = newStatus,
            ActorUserId    = actorUserId,
            ActorType      = actorType,
            Reason         = reason,
            Note           = note,
            ReferenceId    = referenceId,
            ReferenceType  = referenceType,
            MetadataJson   = metadataJson,
            OccurredAtUtc  = DateTimeOffset.UtcNow,
            IsActive       = true,
        };
}
