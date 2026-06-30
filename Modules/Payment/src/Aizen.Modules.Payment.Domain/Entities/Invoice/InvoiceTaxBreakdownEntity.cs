using Aizen.Core.Domain;

namespace Aizen.Modules.Payment.Domain.Entities.Invoice;

/// <summary>
/// Aggregates tax per rate/type for a single invoice.
/// One row per distinct TaxType + TaxRate combination.
///
/// Example: An invoice with two service lines both at %20 KDV → one row (KDV20, 0.20).
/// Example: Mixed rates (KDV20 service + KDV0 exempt item) → two rows.
///
/// Values are derived from InvoiceLineEntity at invoice creation time and stored
/// as a permanent snapshot for legal compliance and audit.
/// </summary>
[DocumentationInfo("Invoice tax breakdown entity",
    "Aggregated tax rows grouped by TaxType and TaxRate. Computed once at invoice creation; immutable after Issue.")]
public sealed class InvoiceTaxBreakdownEntity : AizenEntityWithAudit
{
    public long    InvoiceHeaderId { get; private set; }

    /// <summary>Tax classification code. E.g. "KDV20", "KDV10", "KDV0".</summary>
    public string  TaxType         { get; private set; } = default!;

    /// <summary>The applicable rate as a fraction. E.g. 0.20 for %20 KDV.</summary>
    public decimal TaxRate         { get; private set; }

    /// <summary>Sum of line net amounts (after discount) that this rate applies to.</summary>
    public decimal TaxableAmount   { get; private set; }

    /// <summary>TaxableAmount × TaxRate, rounded to 4 decimal places.</summary>
    public decimal TaxAmount       { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public InvoiceHeaderEntity? InvoiceHeader { get; private set; }

    private InvoiceTaxBreakdownEntity() { }

    public static InvoiceTaxBreakdownEntity Create(
        long    invoiceHeaderId,
        string  taxType,
        decimal taxRate,
        decimal taxableAmount)
    {
        var taxAmount = Math.Round(taxableAmount * taxRate, 4, MidpointRounding.AwayFromZero);

        return new InvoiceTaxBreakdownEntity
        {
            InvoiceHeaderId = invoiceHeaderId,
            TaxType         = taxType.ToUpperInvariant(),
            TaxRate         = taxRate,
            TaxableAmount   = taxableAmount,
            TaxAmount       = taxAmount,
            IsActive        = true,
        };
    }
}
