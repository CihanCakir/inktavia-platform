using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Reporting;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

/// <summary>BE-P12 §15 — append-only financial ledger: idempotent append + paged/aggregate reads (admin reporting).</summary>
public interface IFinancialLedgerRepository
{
    /// <summary>True when a line for <c>(sourceType, sourceRef, accountLine, isReversal)</c> already exists (idempotency pre-check).</summary>
    Task<bool> ExistsAsync(LedgerSourceType sourceType, long sourceRef, LedgerAccountLine accountLine, bool isReversal, CancellationToken ct = default);

    Task AddAsync(FinancialLedgerEntryEntity entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>All entries in [from, to) for a currency (period report aggregation happens in the handler).</summary>
    Task<List<FinancialLedgerEntryEntity>> GetForPeriodAsync(DateTime fromUtc, DateTime toUtc, string? currency, CancellationToken ct = default);

    /// <summary>Paged drill-down with optional filters (account line / source / provider / period).</summary>
    Task<(List<FinancialLedgerEntryEntity> Items, int Total)> GetPagedAsync(
        LedgerAccountLine? accountLine, LedgerSourceType? sourceType, long? providerProfileId,
        DateTime? fromUtc, DateTime? toUtc, string? currency, int skip, int take, CancellationToken ct = default);

    /// <summary>Entries for one source (reconciliation).</summary>
    Task<List<FinancialLedgerEntryEntity>> GetBySourceAsync(LedgerSourceType sourceType, long sourceRef, CancellationToken ct = default);
}
