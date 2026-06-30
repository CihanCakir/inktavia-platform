using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Invoice;

/// <summary>
/// Immutable append-only record of every InvoiceHeader status transition.
/// Written by InvoiceHeaderEntity domain methods. Never updated after insert.
///
/// Provides a complete audit trail for regulatory compliance and dispute resolution.
/// ChangedByUserId is null for system/job-triggered transitions.
/// </summary>
[DocumentationInfo("Invoice status history entity",
    "Append-only audit log of invoice status transitions. Immutable after insert.")]
public sealed class InvoiceStatusHistoryEntity : AizenEntity
{
    public long          InvoiceHeaderId  { get; private set; }
    public InvoiceStatus OldStatus        { get; private set; }
    public InvoiceStatus NewStatus        { get; private set; }
    public string?       Reason           { get; private set; }
    public long?         ChangedByUserId  { get; private set; }
    public DateTime      ChangedAtUtc     { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public InvoiceHeaderEntity? InvoiceHeader { get; private set; }

    private InvoiceStatusHistoryEntity() { }

    internal static InvoiceStatusHistoryEntity Create(
        long          invoiceHeaderId,
        InvoiceStatus oldStatus,
        InvoiceStatus newStatus,
        long?         changedByUserId,
        string?       reason = null)
    {
        return new InvoiceStatusHistoryEntity
        {
            InvoiceHeaderId = invoiceHeaderId,
            OldStatus       = oldStatus,
            NewStatus       = newStatus,
            Reason          = reason,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc    = DateTime.UtcNow,
        };
    }
}
