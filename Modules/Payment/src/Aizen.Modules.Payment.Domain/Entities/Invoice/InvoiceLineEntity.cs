using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Invoice;

/// <summary>
/// A single line item within an invoice document.
/// Multiple lines aggregate into InvoiceHeaderEntity totals.
///
/// Amount invariant: LineTotal = (Quantity × UnitPrice) - DiscountAmount + TaxAmount
///
/// Tax rate must not be hardcoded. Resolved from SystemParameter at invoice creation time
/// and stored as a snapshot in TaxRate for audit permanence.
/// </summary>
[DocumentationInfo("Invoice line entity",
    "Individual line item within an invoice. Stores quantity, pricing, discount, and tax snapshot.")]
public sealed class InvoiceLineEntity : AizenEntityWithAudit
{
    public long             InvoiceHeaderId     { get; private set; }
    public int              LineNumber          { get; private set; }
    public InvoiceLineType  LineType            { get; private set; }
    public string           Description         { get; private set; } = default!;

    /// <summary>Optional product code for physical goods (CargoDry, Commerce).</summary>
    public string? ProductCode          { get; private set; }

    /// <summary>Marine service category code for ServiceRequest lines (e.g. "ENGINE_MAINTENANCE").</summary>
    public string? ServiceCategoryCode  { get; private set; }

    // ── Quantities ────────────────────────────────────────────────────────────
    public decimal Quantity  { get; private set; }
    public string  UnitCode  { get; private set; } = "EACH";   // "EACH", "MONTH", "KIT"

    // ── Amounts ───────────────────────────────────────────────────────────────
    public decimal UnitPrice        { get; private set; }
    public decimal LineSubTotal     { get; private set; }   // Quantity × UnitPrice
    public decimal DiscountAmount   { get; private set; }
    public decimal TaxRate          { get; private set; }   // snapshot; e.g. 0.20 for %20 KDV
    public decimal TaxAmount        { get; private set; }   // (LineSubTotal - DiscountAmount) × TaxRate
    public decimal LineTotal        { get; private set; }   // LineSubTotal - DiscountAmount + TaxAmount

    // ── Source traceability ───────────────────────────────────────────────────
    /// <summary>Business entity type that generated this line (mirrors InvoiceSourceType).</summary>
    public InvoiceSourceType? SourceType { get; private set; }

    /// <summary>FK to the generating record in the source module.</summary>
    public long? SourceId { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public InvoiceHeaderEntity? InvoiceHeader { get; private set; }

    private InvoiceLineEntity() { }

    public static InvoiceLineEntity Create(
        long              invoiceHeaderId,
        int               lineNumber,
        InvoiceLineType   lineType,
        string            description,
        decimal           quantity,
        decimal           unitPrice,
        decimal           taxRate,
        string            unitCode              = "EACH",
        decimal           discountAmount        = 0m,
        string?           productCode           = null,
        string?           serviceCategoryCode   = null,
        InvoiceSourceType? sourceType           = null,
        long?             sourceId              = null)
    {
        var lineSubTotal = quantity * unitPrice;
        var taxableNet   = lineSubTotal - discountAmount;
        var taxAmount    = Math.Round(taxableNet * taxRate, 4, MidpointRounding.AwayFromZero);
        var lineTotal    = taxableNet + taxAmount;

        return new InvoiceLineEntity
        {
            InvoiceHeaderId    = invoiceHeaderId,
            LineNumber         = lineNumber,
            LineType           = lineType,
            Description        = description,
            ProductCode        = productCode,
            ServiceCategoryCode = serviceCategoryCode,
            Quantity           = quantity,
            UnitCode           = unitCode,
            UnitPrice          = unitPrice,
            LineSubTotal       = lineSubTotal,
            DiscountAmount     = discountAmount,
            TaxRate            = taxRate,
            TaxAmount          = taxAmount,
            LineTotal          = lineTotal,
            SourceType         = sourceType,
            SourceId           = sourceId,
            IsActive           = true,
        };
    }
}
