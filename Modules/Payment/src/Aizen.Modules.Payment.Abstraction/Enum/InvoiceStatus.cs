namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Lifecycle status of an InvoiceHeaderEntity.
///
/// State transitions:
///   Draft       → Issued (IssueInvoice)
///   Draft       → Cancelled (CancelInvoice — before number is assigned)
///   Issued      → Sent (SendInvoice)
///   Issued      → Credited (CreateCreditNote)
///   Issued      → Archived (ArchiveInvoice — Admin/SuperAdmin)
///   Sent        → Paid (MarkInvoicePaid — subscription)
///   Sent        → PartiallyPaid (RegisterPartialPayment — subscription)
///   Sent        → Overdue (MarkInvoiceOverdue — job, subscription only)
///   Sent        → Credited (CreateCreditNote)
///   Sent/Paid   → Refunded (RefundInvoice — after gateway refund confirmed)
///   Any Issued+ → Archived (Admin)
///   ExternalSubmissionPending → ExternalSubmitted (provider callback)
///   ExternalSubmissionPending → ExternalRejected (provider callback)
///
/// Terminal statuses: Cancelled, Archived, ExternalRejected (requires re-submission).
/// </summary>
public enum InvoiceStatus
{
    /// <summary>Created but not finalized. Editable. No invoice number assigned yet.</summary>
    Draft                      = 1,

    /// <summary>Finalized. Invoice number assigned. Immutable except SentAt/InternalNote.</summary>
    Issued                     = 2,

    /// <summary>Delivery confirmed — email or notification dispatched.</summary>
    Sent                       = 3,

    /// <summary>Buyer has paid the full amount (subscription/proforma flows).</summary>
    Paid                       = 4,

    /// <summary>Buyer has paid part of the amount (subscription overdue partial payment).</summary>
    PartiallyPaid              = 5,

    /// <summary>DueAt passed without full payment. Subscription invoices only.</summary>
    Overdue                    = 6,

    /// <summary>Voided before Issue transition. No invoice number was consumed.</summary>
    Cancelled                  = 7,

    /// <summary>Full gateway refund issued to buyer.</summary>
    Refunded                   = 8,

    /// <summary>Fully offset by a CreditNote document (OriginalInvoiceId on CRD).</summary>
    Credited                   = 9,

    /// <summary>Processing or gateway error during invoice finalization.</summary>
    Failed                     = 10,

    /// <summary>Issued but not yet submitted to e-invoice/e-archive provider. (Phase 3)</summary>
    ExternalSubmissionPending  = 11,

    /// <summary>Successfully submitted to e-invoice/e-archive provider. (Phase 3)</summary>
    ExternalSubmitted          = 12,

    /// <summary>Submission rejected by e-invoice/e-archive provider. Retry required. (Phase 3)</summary>
    ExternalRejected           = 13,

    /// <summary>Admin-archived historical record. Read-only reference.</summary>
    Archived                   = 14,
}
