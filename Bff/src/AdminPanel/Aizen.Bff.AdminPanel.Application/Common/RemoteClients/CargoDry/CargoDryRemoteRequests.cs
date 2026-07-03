namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;

// ── Admin Request DTOs ────────────────────────────────────────────────────────

[DocumentationInfo("Generate batch BFF request", "Admin request to generate a new CargoDry kit batch.")]
public sealed class GenerateBatchBffRequest
{
    public string  ProductCode     { get; init; } = default!;
    public int     Count           { get; init; }
    public string? BatchLabel      { get; init; }
    public string? WarehouseCode   { get; init; }
    public string? ProductionNotes { get; init; }
}

[DocumentationInfo("Create product BFF request", "Admin request to register a new CargoDry product in the catalog.")]
public sealed class CreateProductBffRequest
{
    public string  ProductCode    { get; init; } = default!;
    public string  Name           { get; init; } = default!;
    public string  Description    { get; init; } = default!;
    public int     ValidityDays   { get; init; }
    public decimal RetailPrice    { get; init; }
    public string  CurrencyCode   { get; init; } = default!;
    public bool    HasSmartDevice { get; init; }
    public string? DeviceType     { get; init; }
}

[DocumentationInfo("Update product BFF request", "Admin request to update an existing CargoDry product.")]
public sealed class UpdateProductBffRequest
{
    public string  Name           { get; init; } = default!;
    public string  Description    { get; init; } = default!;
    public int     ValidityDays   { get; init; }
    public decimal RetailPrice    { get; init; }
    public string  CurrencyCode   { get; init; } = default!;
    public bool    HasSmartDevice { get; init; }
    public string? DeviceType     { get; init; }
    public bool    IsActive       { get; init; }
}

[DocumentationInfo("Revoke kit BFF request", "Admin request to revoke a CargoDry kit with a reason.")]
public sealed class RevokeKitBffRequest
{
    public string Reason { get; init; } = default!;
}

[DocumentationInfo("Extend kit BFF request", "Admin request to extend kit validity by adding days.")]
public sealed class ExtendKitBffRequest
{
    public int AddedDays { get; init; }
}

[DocumentationInfo("Renew kit BFF request", "Admin request to renew an expiring kit.")]
public sealed class RenewKitBffRequest
{
    public int     AddedDays  { get; init; }
    public string? PaymentRef { get; init; }
}

[DocumentationInfo("Revoke batch BFF request", "Admin request to revoke a CargoDry batch and cascade-revoke its Available kits.")]
public sealed class RevokeBatchBffRequest
{
    public string Reason { get; init; } = default!;
}

[DocumentationInfo("Transfer kit BFF request", "Admin request to transfer an Activated kit to a new owner and vessel.")]
public sealed class TransferKitBffRequest
{
    public long NewUserId   { get; init; }
    public long NewVesselId { get; init; }
}

// ── Consignment Agreement Request DTOs ────────────────────────────────────────

[DocumentationInfo("Create consignment agreement BFF request",
    "Admin request to create a new CargoDry consignment agreement in Draft status.")]
public sealed class CreateConsignmentAgreementBffRequest
{
    public string    AgreementCode           { get; init; } = default!;
    public long      ProviderProfileId       { get; init; }
    public string    ProductCode             { get; init; } = default!;
    public decimal   ConsignmentRate         { get; init; }
    public decimal   MinimumSettlementAmount { get; init; }
    public string    CurrencyCode            { get; init; } = "TRY";
    public int       MaxKitCount             { get; init; }
    public DateTime  StartDateUtc            { get; init; }
    public DateTime? EndDateUtc              { get; init; }
    public string?   TermsDocumentRef        { get; init; }
    public string?   Notes                   { get; init; }
}

[DocumentationInfo("Update consignment agreement BFF request",
    "Admin request to update commercial terms of a Draft or Suspended agreement.")]
public sealed class UpdateConsignmentAgreementBffRequest
{
    public decimal   ConsignmentRate         { get; init; }
    public decimal   MinimumSettlementAmount { get; init; }
    public string    CurrencyCode            { get; init; } = "TRY";
    public int       MaxKitCount             { get; init; }
    public DateTime  StartDateUtc            { get; init; }
    public DateTime? EndDateUtc              { get; init; }
    public string?   TermsDocumentRef        { get; init; }
    public string?   Notes                   { get; init; }
}

[DocumentationInfo("Consignment agreement reason request", "Carries a reason string for Suspend/Terminate actions.")]
public sealed class ConsignmentAgreementReasonBffRequest
{
    public string Reason { get; init; } = default!;
}

// ── Provider Inventory Request DTOs ──────────────────────────────────────────

[DocumentationInfo("Allocate batch to provider BFF request",
    "Admin request to allocate a batch and all its available kits to a provider. Phase 2.")]
public sealed class AllocateBatchToProviderBffRequest
{
    public string                  BatchCode              { get; init; } = default!;
    public long                    ProviderProfileId      { get; init; }
    public int                     CommercialModel        { get; init; }
    public int                     SalesChannel           { get; init; }
    public long?                   ConsignmentAgreementId { get; init; }
    public long?                   WarehouseId            { get; init; }
    public string?                 Note                   { get; init; }
}

[DocumentationInfo("Adjust provider inventory BFF request",
    "Admin request for manual stock correction on a provider inventory record. Phase 2.")]
public sealed class AdjustProviderInventoryBffRequest
{
    public long    ProviderProfileId  { get; init; }
    public string  ProductCode        { get; init; } = default!;
    public string? BatchCode          { get; init; }
    public int     AdjustmentQuantity { get; init; }
    public string  Reason             { get; init; } = default!;
}

// ── Onboarding Request DTOs ───────────────────────────────────────────────────

[DocumentationInfo("Validate kit BFF request", "Public request to validate a CargoDry kit serial/QR before activation.")]
public sealed class ValidateKitBffRequest
{
    public string  SerialNumber { get; init; } = default!;
    public string  BatchCode    { get; init; } = default!;
    public string? Signature    { get; init; }
}

[DocumentationInfo("Activate kit BFF request", "Authenticated user request to activate a CargoDry kit on a vessel.")]
public sealed class ActivateKitBffRequest
{
    public string ActivationToken { get; init; } = default!;
    public long   VesselId        { get; init; }
}

// ── Phase 4A: Financial Resolution Request DTOs ────────────────────────────────

[DocumentationInfo("Resolve attribution financials BFF request",
    "Admin request to resolve SalePrice, CommissionRate, ProviderShareAmount, and PlatformShareAmount " +
    "on a CargoDry sales attribution record. Phase 4A (July 2026).")]
public sealed class ResolveAttributionFinancialsBffRequest
{
    public decimal  SalePrice              { get; init; }
    public string   CurrencyCode           { get; init; } = default!;
    public decimal? CommissionRateOverride  { get; init; }
    public long     ResolvedByUserId       { get; init; }
    public string?  ResolutionNote         { get; init; }
}

[DocumentationInfo("Resolve monthly settlement BFF request",
    "Admin request to finalize a monthly sell-through settlement: " +
    "verifies all attribution financials resolved, recalculates totals, marks ReadyForSettlement. " +
    "Phase 4A (July 2026).")]
public sealed class ResolveMonthlySettlementBffRequest
{
    public long    ResolvedByUserId { get; init; }
    public string? ResolutionNote   { get; init; }
}

[DocumentationInfo("Prepare settlement payment BFF request",
    "Admin request to create a payout preparation record in the Payment module for a " +
    "ReadyForSettlement sell-through settlement and transition it to Scheduled status. " +
    "Idempotent — safe to call multiple times. " +
    "Phase 4B (July 2026).")]
public sealed class PrepareCargoDrySettlementPaymentBffRequest
{
    public long    PreparedByUserId { get; init; }
    public string? PreparationNote  { get; init; }
}

// ── Phase 4C: Settlement Invoice Preparation Request DTOs ─────────────────────

[DocumentationInfo("Prepare settlement invoice BFF request",
    "Admin request to create a Draft ProviderSettlementStatement invoice in the Payment module " +
    "for a Scheduled sell-through settlement. Settlement status remains Scheduled after preparation. " +
    "Idempotent — safe to call multiple times. " +
    "Phase 4C (July 2026).")]
public sealed class PrepareCargoDrySettlementInvoiceBffRequest
{
    public long    PreparedByUserId { get; init; }
    public string? PreparationNote  { get; init; }
}

// ── Phase 4D: Payout Lifecycle Request DTOs ───────────────────────────────────

[DocumentationInfo("Approve CargoDry settlement payout BFF request",
    "Admin request to transition the linked PayoutRecord from Pending → Approved. " +
    "Settlement status remains Scheduled. No gateway call. Phase 4D (July 2026).")]
public sealed class ApproveCargoDrySettlementPayoutBffRequest
{
    public long    ApprovedByUserId { get; init; }
    public string? Note             { get; init; }
}

[DocumentationInfo("Mark CargoDry settlement payout processing BFF request",
    "Admin request to transition the linked PayoutRecord to Processing status (optional step). " +
    "Settlement status remains Scheduled. No gateway call. Phase 4D (July 2026).")]
public sealed class MarkCargoDrySettlementPayoutProcessingBffRequest
{
    public long    ProcessedByUserId  { get; init; }
    public string? ExternalReference  { get; init; }
    public string? Note               { get; init; }
}

[DocumentationInfo("Complete CargoDry settlement payout BFF request",
    "Admin request to record a successful manual payout and close the settlement as Settled. " +
    "ManualPaymentReference is required. Idempotent if already Settled. " +
    "This is the ONLY request that advances settlement status to Settled. " +
    "Phase 4D (July 2026).")]
public sealed class CompleteCargoDrySettlementPayoutBffRequest
{
    public long   CompletedByUserId      { get; init; }
    public string ManualPaymentReference { get; init; } = default!;
    public string? Note                  { get; init; }
}

[DocumentationInfo("Fail CargoDry settlement payout BFF request",
    "Admin request to record a payout failure. FailureReason is required. " +
    "Settlement status remains Scheduled, allowing admin to retry. " +
    "Cannot fail an already-Settled settlement. Phase 4D (July 2026).")]
public sealed class FailCargoDrySettlementPayoutBffRequest
{
    public long    FailedByUserId    { get; init; }
    public string  FailureReason     { get; init; } = default!;
    public string? ExternalReference { get; init; }
    public string? Note              { get; init; }
}
