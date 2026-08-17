using Aizen.Core.Domain;

namespace Aizen.Modules.Payment.Domain.Entities.Invoice;

/// <summary>
/// Tracks the e-invoice / e-archive submission lifecycle for a single invoice.
/// One record per InvoiceHeaderEntity (1:1 relationship).
///
/// ── MVP scope ────────────────────────────────────────────────────────────────
///  Table is created in Phase 1A migration.
///  No rows are inserted until Phase 3 (GIB e-arşiv integration).
///  The entity exists here to keep the schema future-proof without blocking MVP delivery.
///
/// ── Phase 3 providers (planned) ──────────────────────────────────────────────
///  "GIB-EARSHIV"  — Direct GIB e-archive API
///  "GIB-EFATURA"  — GIB e-invoice (requires UBL-TR 2.1 format)
///  "LOGO"         — Logo Tiger middleware
///  "PARASUT"      — Paraşüt fintech middleware
///
/// ── Status values (string, not enum — provider-agnostic) ─────────────────────
///  "PENDING"    — Queued, not yet submitted
///  "SUBMITTED"  — HTTP POST sent to provider
///  "APPROVED"   — Provider confirmed acceptance
///  "REJECTED"   — Provider returned validation error (see ErrorMessage + RawResponsePayload)
/// </summary>
[DocumentationInfo("Invoice external integration entity",
    "E-invoice/e-archive submission tracker (Phase 3). Table exists in MVP schema; rows created in Phase 3.")]
public sealed class InvoiceExternalIntegrationEntity : AizenEntity
{
    public long    InvoiceHeaderId     { get; private set; }

    /// <summary>
    /// Provider identifier key. E.g. "GIB-EARSHIV", "LOGO", "PARASUT".
    /// Determines serialization format and API endpoint.
    /// </summary>
    public string  Provider            { get; private set; } = default!;

    /// <summary>Provider-assigned document UUID returned after successful submission.</summary>
    public string? ExternalInvoiceId   { get; private set; }

    /// <summary>
    /// Current provider-side status string.
    /// Values: "PENDING", "SUBMITTED", "APPROVED", "REJECTED".
    /// </summary>
    public string  Status              { get; private set; } = "PENDING";

    public string? ErrorMessage        { get; private set; }

    /// <summary>Full UBL-TR XML or JSON payload sent to the provider (audit storage).</summary>
    public string? RawRequestPayload   { get; private set; }

    /// <summary>Full provider response body (audit storage).</summary>
    public string? RawResponsePayload  { get; private set; }

    public DateTime  CreatedAtUtc      { get; private set; }
    public DateTime? UpdatedAtUtc      { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public InvoiceHeaderEntity? InvoiceHeader { get; private set; }

    private InvoiceExternalIntegrationEntity() { }

    public static InvoiceExternalIntegrationEntity Create(long invoiceHeaderId, string provider)
    {
        return new InvoiceExternalIntegrationEntity
        {
            InvoiceHeaderId = invoiceHeaderId,
            Provider        = provider.ToUpperInvariant(),
            Status          = "PENDING",
            CreatedAtUtc    = DateTime.UtcNow,
        };
    }

    public void MarkSubmitted(string rawRequestPayload)
    {
        Status             = "SUBMITTED";
        RawRequestPayload  = rawRequestPayload;
        UpdatedAtUtc       = DateTime.UtcNow;
    }

    public void MarkApproved(string externalInvoiceId, string? rawResponsePayload = null)
    {
        ExternalInvoiceId  = externalInvoiceId;
        Status             = "APPROVED";
        RawResponsePayload = rawResponsePayload;
        UpdatedAtUtc       = DateTime.UtcNow;
    }

    public void MarkRejected(string errorMessage, string? rawResponsePayload = null)
    {
        Status             = "REJECTED";
        ErrorMessage       = errorMessage;
        RawResponsePayload = rawResponsePayload;
        UpdatedAtUtc       = DateTime.UtcNow;
    }
}
