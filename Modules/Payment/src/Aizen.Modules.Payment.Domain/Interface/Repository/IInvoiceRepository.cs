using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Invoice;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

/// <summary>
/// Repository contract for InvoiceHeaderEntity and its aggregate children.
///
/// Load strategy:
///   GetByIdAsync         — header only (list views, status checks)
///   GetByIdFullAsync     — header + Lines + TaxBreakdowns + StatusHistory (detail view, PDF generation)
///   GetByInvoiceNumberAsync — header only (lookup by formatted number)
///
/// InvoiceLineEntity, InvoiceTaxBreakdownEntity, and InvoiceStatusHistoryEntity
/// are always accessed through the InvoiceHeaderEntity aggregate root.
/// They have no standalone repository.
/// </summary>
public interface IInvoiceRepository
{
    // ── Lookups ───────────────────────────────────────────────────────────────

    Task<InvoiceHeaderEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Loads header with Lines, TaxBreakdowns, and StatusHistory.</summary>
    Task<InvoiceHeaderEntity?> GetByIdFullAsync(long id, CancellationToken ct = default);

    Task<InvoiceHeaderEntity?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default);

    Task<InvoiceHeaderEntity?> GetByTransactionIdAsync(long transactionId, CancellationToken ct = default);

    // ── Paged lists ───────────────────────────────────────────────────────────

    /// <summary>Admin paged list with all filter options.</summary>
    Task<(List<InvoiceHeaderEntity> Items, int Total)> GetPagedAsync(
        InvoiceType?       type,
        InvoiceStatus?     status,
        string?            prefix,
        long?              buyerUserId,
        DateTime?          fromDate,
        DateTime?          toDate,
        string?            search,      // matches InvoiceNumber or BuyerName
        int                skip,
        int                take,
        CancellationToken  ct = default);

    /// <summary>Buyer-facing paged list — filtered to caller's IdentityId only.</summary>
    Task<(List<InvoiceHeaderEntity> Items, int Total)> GetByBuyerPagedAsync(
        long               buyerUserId,
        InvoiceStatus?     status,
        InvoiceType?       type,
        int                skip,
        int                take,
        CancellationToken  ct = default);

    /// <summary>All CreditNote invoices linked to a given original invoice.</summary>
    Task<List<InvoiceHeaderEntity>> GetCreditNotesByOriginalIdAsync(
        long originalInvoiceId, CancellationToken ct = default);

    /// <summary>All invoices linked to a specific payment transaction.</summary>
    Task<List<InvoiceHeaderEntity>> GetByTransactionIdAllAsync(
        long transactionId, CancellationToken ct = default);

    /// <summary>
    /// Sent subscription invoices whose DueAt has passed.
    /// Used by InvoiceOverdueCheckJob.
    /// </summary>
    Task<List<InvoiceHeaderEntity>> GetSentSubscriptionOverdueAsync(
        DateTime dueBefore, CancellationToken ct = default);

    // ── Mutations ─────────────────────────────────────────────────────────────

    Task AddAsync(InvoiceHeaderEntity invoice, CancellationToken ct = default);
    void Update(InvoiceHeaderEntity invoice);
    Task SaveChangesAsync(CancellationToken ct = default);
}
