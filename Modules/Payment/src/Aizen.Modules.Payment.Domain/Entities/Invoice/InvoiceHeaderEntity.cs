using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Invoice;

/// <summary>
/// Root document of the Invoice subsystem.
///
/// ── Lifecycle ────────────────────────────────────────────────────────────────
///  Draft → Issued (number assigned) → Sent → Paid / Overdue / Credited / Archived
///  Draft → Cancelled  (before Issue; no number consumed)
///  Any Issued+ → Credited (CreateCreditNote)
///  Any Issued+ → Archived (Admin)
///
/// ── Immutability ─────────────────────────────────────────────────────────────
///  Once Status ≥ Issued, all financial fields and party info are immutable.
///  Corrections go through CreateCreditNote (new CRD document, OriginalInvoiceId set).
///
/// ── Numbering ────────────────────────────────────────────────────────────────
///  InvoiceNumber is null while Draft.
///  Assigned by InvoiceNumberGenerator only at the Issue transition.
///  Format: {PREFIX}-{YYYY}-{MM}-{NNNNNN}
///  Prefixes: INV, COM, SUB, CRD
///
/// ── Tax / KDV ────────────────────────────────────────────────────────────────
///  KDV is never hardcoded. Read from SystemParameter["PAYMENT_KDV_RATE_DEFAULT"].
///  TaxAmount is stored as a snapshot at invoice creation time.
/// </summary>
[DocumentationInfo("Invoice header entity",
    "Root invoice document. Covers all 6 invoice scenarios: SalesInvoice, CommissionInvoice, " +
    "SubscriptionInvoice, CargoDryInvoice, CreditNote, RefundInvoice.")]
public sealed class InvoiceHeaderEntity : AizenEntityWithAudit
{
    // ── Identity ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Formatted invoice number (e.g. INV-2026-06-000001).
    /// Null while Status = Draft. Assigned at Issue transition only.
    /// </summary>
    public string? InvoiceNumber    { get; private set; }

    public InvoiceType       InvoiceType       { get; private set; }
    public CommercialModel   CommercialModel   { get; private set; }
    public BillingMode       BillingMode       { get; private set; }
    public InvoiceStatus     Status            { get; private set; }

    // ── Source linkage ────────────────────────────────────────────────────────
    /// <summary>The domain context that triggered invoice creation.</summary>
    public InvoiceSourceType SourceType        { get; private set; }
    public long?             SourceId          { get; private set; }

    // ── Payment linkage ───────────────────────────────────────────────────────
    /// <summary>FK to PaymentTransactionEntity (cross-module by Id, no EF FK constraint).</summary>
    public long? PaymentTransactionId   { get; private set; }

    /// <summary>FK to the escrow release record if this is a COM invoice.</summary>
    public long? PaymentReleaseId       { get; private set; }

    /// <summary>FK to a commission calculation snapshot (future reporting use).</summary>
    public long? CommissionCalculationId { get; private set; }

    /// <summary>FK to PayoutRecordEntity for COM invoices.</summary>
    public long? ProviderPayoutId       { get; private set; }

    /// <summary>FK to ProviderPlanSubscriptionEntity or ParticipantPlanSubscriptionEntity.</summary>
    public long? UserSubscriptionId     { get; private set; }

    /// <summary>
    /// For CreditNote / RefundInvoice: FK to the original InvoiceHeaderEntity being credited.
    /// </summary>
    public long? OriginalInvoiceId      { get; private set; }

    // ── Seller party (snapshot at Issue) ─────────────────────────────────────
    /// <summary>Null = platform (Inktavia) is the seller. Non-null = provider is seller (Phase 4).</summary>
    public long?   SellerUserId    { get; private set; }
    public string  SellerName      { get; private set; } = default!;
    public string? SellerTaxNumber { get; private set; }
    public string? SellerTaxOffice { get; private set; }
    public string? SellerAddress   { get; private set; }

    // ── Buyer party (snapshot at Issue) ──────────────────────────────────────
    /// <summary>Cross-module reference to Identity. Not enforced as EF FK.</summary>
    public long?   BuyerUserId    { get; private set; }
    public string  BuyerName      { get; private set; } = default!;
    public string? BuyerTaxNumber { get; private set; }
    public string? BuyerTaxOffice { get; private set; }
    public string? BuyerAddress   { get; private set; }

    // ── Amounts ───────────────────────────────────────────────────────────────
    public string  Currency        { get; private set; } = "TRY";
    public decimal SubTotalAmount  { get; private set; }
    public decimal DiscountAmount  { get; private set; }
    public decimal TaxableAmount   { get; private set; }
    public decimal TaxAmount       { get; private set; }
    public decimal TotalAmount     { get; private set; }

    /// <summary>Amount collected so far (used for subscription partial payments).</summary>
    public decimal PaidAmount      { get; private set; }

    /// <summary>Computed: TotalAmount - PaidAmount. Never negative.</summary>
    public decimal RemainingAmount { get; private set; }

    // ── Dates ─────────────────────────────────────────────────────────────────
    public DateTime? IssueDateUtc    { get; private set; }
    public DateTime? DueDateUtc      { get; private set; }
    public DateTime? PaidAtUtc       { get; private set; }
    public DateTime? CancelledAtUtc  { get; private set; }

    // ── External / PDF ────────────────────────────────────────────────────────
    /// <summary>Quick-access external e-invoice/e-archive reference (denormalized from ExternalIntegration).</summary>
    public string? ExternalInvoiceId       { get; private set; }
    public string? ExternalInvoiceProvider { get; private set; }

    /// <summary>File storage reference for the generated PDF (Phase 2).</summary>
    public string? PdfFileRef { get; private set; }

    /// <summary>Internal admin-only notes. Never shown on buyer-facing PDF.</summary>
    public string? Notes { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    private List<InvoiceLineEntity>          _lines        = new();
    private List<InvoiceTaxBreakdownEntity>  _taxBreakdowns = new();
    private List<InvoiceStatusHistoryEntity> _statusHistory = new();

    public IReadOnlyCollection<InvoiceLineEntity>          Lines         => _lines.AsReadOnly();
    public IReadOnlyCollection<InvoiceTaxBreakdownEntity>  TaxBreakdowns => _taxBreakdowns.AsReadOnly();
    public IReadOnlyCollection<InvoiceStatusHistoryEntity> StatusHistory => _statusHistory.AsReadOnly();

    // One-to-one (optional); loaded explicitly
    public InvoiceExternalIntegrationEntity? ExternalIntegration { get; private set; }

    private InvoiceHeaderEntity() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a Draft invoice. InvoiceNumber is null until Issue.
    /// All financial amounts must be pre-calculated by InvoiceFactory before calling.
    /// KDV/tax must NOT be hardcoded by callers — resolve from SystemParameter first.
    /// </summary>
    public static InvoiceHeaderEntity CreateDraft(
        InvoiceType     invoiceType,
        CommercialModel commercialModel,
        InvoiceSourceType sourceType,
        long?           sourceId,
        string          sellerName,
        string          buyerName,
        string          currency,
        decimal         subTotalAmount,
        decimal         discountAmount,
        decimal         taxableAmount,
        decimal         taxAmount,
        decimal         totalAmount,
        long?           sellerUserId          = null,
        string?         sellerTaxNumber       = null,
        string?         sellerTaxOffice       = null,
        string?         sellerAddress         = null,
        long?           buyerUserId           = null,
        string?         buyerTaxNumber        = null,
        string?         buyerTaxOffice        = null,
        string?         buyerAddress          = null,
        long?           paymentTransactionId  = null,
        long?           paymentReleaseId      = null,
        long?           commissionCalculationId = null,
        long?           providerPayoutId      = null,
        long?           userSubscriptionId    = null,
        long?           originalInvoiceId     = null,
        DateTime?       dueDateUtc            = null,
        string?         notes                 = null)
    {
        return new InvoiceHeaderEntity
        {
            InvoiceType              = invoiceType,
            CommercialModel          = commercialModel,
            BillingMode              = BillingMode.Reseller, // MVP default; Phase 4 adds AgencyPrincipalInvoices
            Status                   = InvoiceStatus.Draft,
            SourceType               = sourceType,
            SourceId                 = sourceId,
            SellerUserId             = sellerUserId,
            SellerName               = sellerName,
            SellerTaxNumber          = sellerTaxNumber,
            SellerTaxOffice          = sellerTaxOffice,
            SellerAddress            = sellerAddress,
            BuyerUserId              = buyerUserId,
            BuyerName                = buyerName,
            BuyerTaxNumber           = buyerTaxNumber,
            BuyerTaxOffice           = buyerTaxOffice,
            BuyerAddress             = buyerAddress,
            Currency                 = currency.ToUpperInvariant(),
            SubTotalAmount           = subTotalAmount,
            DiscountAmount           = discountAmount,
            TaxableAmount            = taxableAmount,
            TaxAmount                = taxAmount,
            TotalAmount              = totalAmount,
            PaidAmount               = 0m,
            RemainingAmount          = totalAmount,
            PaymentTransactionId     = paymentTransactionId,
            PaymentReleaseId         = paymentReleaseId,
            CommissionCalculationId  = commissionCalculationId,
            ProviderPayoutId         = providerPayoutId,
            UserSubscriptionId       = userSubscriptionId,
            OriginalInvoiceId        = originalInvoiceId,
            DueDateUtc               = dueDateUtc,
            Notes                    = notes,
            IsActive                 = true,
        };
    }

    // ── Domain transitions ────────────────────────────────────────────────────

    /// <summary>
    /// Assigns invoice number and transitions to Issued.
    /// Must only be called by IssueInvoiceCommandHandler after number is generated.
    /// </summary>
    public void Issue(string invoiceNumber, long? issuedByUserId)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException(
                $"Invoice {Id} cannot be issued from status {Status}. Expected: Draft.");

        InvoiceNumber = invoiceNumber;
        Status        = InvoiceStatus.Issued;
        IssueDateUtc  = DateTime.UtcNow;

        RecordStatusChange(InvoiceStatus.Draft, InvoiceStatus.Issued, issuedByUserId);
    }

    /// <summary>Transitions Issued → Sent after email/notification dispatch.</summary>
    public void MarkSent(long? triggeredByUserId)
    {
        if (Status != InvoiceStatus.Issued)
            throw new InvalidOperationException(
                $"Invoice {Id} cannot be marked Sent from status {Status}. Expected: Issued.");

        var prev = Status;
        Status = InvoiceStatus.Sent;
        RecordStatusChange(prev, InvoiceStatus.Sent, triggeredByUserId);
    }

    /// <summary>Transitions Draft → Cancelled. Consumes no invoice number.</summary>
    public void Cancel(long? cancelledByUserId, string? reason = null)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException(
                $"Invoice {Id} can only be cancelled from Draft status. Current: {Status}.");

        var prev      = Status;
        Status        = InvoiceStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        RecordStatusChange(prev, InvoiceStatus.Cancelled, cancelledByUserId, reason);
    }

    /// <summary>Transitions any Issued+ status → Credited after a CreditNote is created.</summary>
    public void MarkCredited(long? triggeredByUserId)
    {
        if (Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled or InvoiceStatus.Archived)
            throw new InvalidOperationException(
                $"Invoice {Id} cannot be credited from status {Status}.");

        var prev = Status;
        Status   = InvoiceStatus.Credited;
        RecordStatusChange(prev, InvoiceStatus.Credited, triggeredByUserId);
    }

    /// <summary>Transitions Sent subscription invoice → Overdue (called by job).</summary>
    public void MarkOverdue(long? triggeredByUserId)
    {
        if (Status != InvoiceStatus.Sent)
            throw new InvalidOperationException(
                $"Invoice {Id} cannot be marked Overdue from status {Status}. Expected: Sent.");

        var prev = Status;
        Status   = InvoiceStatus.Overdue;
        RecordStatusChange(prev, InvoiceStatus.Overdue, triggeredByUserId, "Due date passed.");
    }

    /// <summary>Archives the invoice. SuperAdmin only. Terminal state.</summary>
    public void Archive(long? archivedByUserId, string? reason = null)
    {
        if (Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            throw new InvalidOperationException(
                $"Invoice {Id} cannot be archived from status {Status}.");

        var prev = Status;
        Status   = InvoiceStatus.Archived;
        RecordStatusChange(prev, InvoiceStatus.Archived, archivedByUserId, reason);
    }

    // ── Mutation helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Adds a line item to this draft invoice.
    /// Must only be called while Status == Draft; lines are immutable after Issue.
    /// </summary>
    public void AddLine(InvoiceLineEntity line)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException(
                $"Invoice {Id} is not in Draft status. Lines cannot be modified after Issue.");
        _lines.Add(line);
    }

    /// <summary>
    /// Adds a tax breakdown row to this draft invoice.
    /// Must only be called while Status == Draft.
    /// </summary>
    public void AddTaxBreakdown(InvoiceTaxBreakdownEntity breakdown)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException(
                $"Invoice {Id} is not in Draft status. Tax breakdowns cannot be modified after Issue.");
        _taxBreakdowns.Add(breakdown);
    }

    public void SetPdfRef(string pdfFileRef)          => PdfFileRef = pdfFileRef;
    public void UpdateNotes(string? notes)             => Notes      = notes;

    public void SetExternalRef(string provider, string externalId)
    {
        ExternalInvoiceProvider = provider;
        ExternalInvoiceId       = externalId;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RecordStatusChange(
        InvoiceStatus from, InvoiceStatus to, long? changedByUserId, string? reason = null)
    {
        _statusHistory.Add(InvoiceStatusHistoryEntity.Create(Id, from, to, changedByUserId, reason));
    }
}
