using Aizen.Core.Domain;

namespace Aizen.Modules.Payment.Domain.Entities.Invoice;

/// <summary>
/// Concurrency-safe invoice number counter.
/// One row per (Prefix, Year, Month) combination.
///
/// ── Numbering format ──────────────────────────────────────────────────────────
///  {PREFIX}-{YYYY}-{MM}-{NNNNNN}
///  Examples: INV-2026-06-000001 | COM-2026-06-000042 | SUB-2026-11-000007 | CRD-2026-06-000003
///
/// ── Concurrency strategy (Phase 1B) ──────────────────────────────────────────
///  Use optimistic concurrency via EF row version (UpdatedAtUtc used as check token
///  in InvoiceNumberGenerator). Retry up to 5 times with exponential backoff.
///  Row is lazily created on first invoice of each (Prefix, Year, Month) combination.
///
/// ── Anti-gap rule ─────────────────────────────────────────────────────────────
///  LastNumber is incremented ONLY at the Issue transition, never at Draft creation.
///  Cancelled drafts consume no sequence numbers.
///  LastNumber is monotonically increasing within (Prefix, Year, Month) — never decremented.
/// </summary>
[DocumentationInfo("Invoice number sequence entity",
    "Per-prefix per-month invoice number counter. Lazily created on first invoice of the month.")]
public sealed class InvoiceNumberSequenceEntity : AizenEntity
{
    /// <summary>Invoice prefix: "INV", "COM", "SUB", "CRD".</summary>
    public string Prefix      { get; private set; } = default!;

    public int Year           { get; private set; }
    public int Month          { get; private set; }

    /// <summary>Last assigned sequence number. Starts at 0; first issued invoice gets 1.</summary>
    public int LastNumber     { get; private set; }

    /// <summary>Timestamp of last increment. Used as a lightweight concurrency check in the generator.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private InvoiceNumberSequenceEntity() { }

    /// <summary>Creates a new sequence row for the given prefix/year/month combination.</summary>
    public static InvoiceNumberSequenceEntity Create(string prefix, int year, int month)
    {
        return new InvoiceNumberSequenceEntity
        {
            Prefix       = prefix.ToUpperInvariant(),
            Year         = year,
            Month        = month,
            LastNumber   = 0,
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Increments LastNumber and returns the new value.
    /// Must be called inside a transaction. UpdatedAtUtc is updated for concurrency tracking.
    /// </summary>
    public int Increment()
    {
        LastNumber  += 1;
        UpdatedAtUtc = DateTime.UtcNow;
        return LastNumber;
    }

    /// <summary>Formats the next invoice number string after incrementing.</summary>
    public string FormatNumber() =>
        $"{Prefix}-{Year:0000}-{Month:00}-{LastNumber:000000}";
}
