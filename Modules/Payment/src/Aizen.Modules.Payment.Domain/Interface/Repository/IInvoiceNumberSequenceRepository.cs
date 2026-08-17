using Aizen.Modules.Payment.Domain.Entities.Invoice;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

/// <summary>
/// Repository contract for InvoiceNumberSequenceEntity.
///
/// Usage pattern:
///   1. Call GetAsync(prefix, year, month) inside a transaction.
///   2. If null → call AddAsync with InvoiceNumberSequenceEntity.Create(...)
///   3. Call Increment() on the entity.
///   4. Call Update() then SaveChangesAsync() — must remain inside the same transaction.
///
/// Concurrency note:
///   InvoiceNumberService (Phase 1B) wraps these calls in a retry loop
///   with exponential backoff to handle EF optimistic concurrency exceptions.
///   This repository does not handle retries itself.
/// </summary>
public interface IInvoiceNumberSequenceRepository
{
    /// <summary>
    /// Retrieves the sequence row for the given prefix/year/month.
    /// Returns null when no invoice has been issued for this combination yet.
    /// </summary>
    Task<InvoiceNumberSequenceEntity?> GetAsync(
        string prefix, int year, int month, CancellationToken ct = default);

    Task AddAsync(InvoiceNumberSequenceEntity sequence, CancellationToken ct = default);

    void Update(InvoiceNumberSequenceEntity sequence);

    Task SaveChangesAsync(CancellationToken ct = default);
}
