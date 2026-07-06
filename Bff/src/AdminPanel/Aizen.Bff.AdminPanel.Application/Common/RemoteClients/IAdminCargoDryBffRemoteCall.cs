using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("CargoDry admin BFF remote call",
    "Defines BFF-to-CargoDry calls. Auth headers (Authorization + X-Aizen-User-Token) are " +
    "injected automatically by AdminPanelBffAuthDelegatingHandler. " +
    "Public onboarding endpoints have no auth requirement but still pass through the handler safely.")]
public interface IAdminCargoDryBffRemoteCall : IAizenRemoteCall
{
    // ── Admin Kit Management ──────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/kits")]
    Task<CargoDryKitListBffDto> GetKitsAsync(
        [Query] string? status,
        [Query] string? search,
        [Query] long?   vesselId,
        [Query] long?   ownerUserId,
        [Query] string? batchCode,
        [Query] int     page,
        [Query] int     pageSize,
        CancellationToken ct = default);

    // ── Admin Kit Detail + Lookup (Phase 8B) ─────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/kits/{id}")]
    Task<GetCargoDryKitDetailBffResult> GetKitDetailAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/kits/lookup")]
    Task<LookupCargoDryKitAdminBffResult> LookupKitAsync(
        [Query] string q,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/stats")]
    Task<CargoDryStatsBffDto> GetStatsAsync(
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/batches/generate")]
    Task<GenerateBatchBffResultDto> GenerateBatchAsync(
        [AizenRemoteCallBody] GenerateBatchBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/kits/{id}/revoke")]
    Task<RevokeKitBffResponse> RevokeKitAsync(
        long id,
        [AizenRemoteCallBody] RevokeKitBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/batches/{batchCode}/revoke")]
    Task<RevokeBatchBffResponse> RevokeBatchAsync(
        string batchCode,
        [AizenRemoteCallBody] RevokeBatchBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/kits/{id}/transfer")]
    Task<TransferKitBffResponse> TransferKitAsync(
        long id,
        [AizenRemoteCallBody] TransferKitBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/products")]
    Task<List<CargoDryProductBffDto>> GetProductsAsync(
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/products/{productCode}")]
    Task<CargoDryProductBffDto?> GetProductDetailAsync(
        string productCode,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/products")]
    Task<CargoDryProductBffDto> CreateProductAsync(
        [AizenRemoteCallBody] CreateProductBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/cargodry/admin/products/{productCode}")]
    Task<CargoDryProductBffDto> UpdateProductAsync(
        string productCode,
        [AizenRemoteCallBody] UpdateProductBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/batches")]
    Task<CargoDryBatchListBffDto> GetBatchesAsync(
        [Query] int page,
        [Query] int pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/batches/{batchCode}")]
    Task<CargoDryBatchBffDto> GetBatchByCodeAsync(
        string batchCode,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/kits/{id}/renew")]
    Task<CargoDryKitBffDto> RenewKitAsync(
        long id,
        [AizenRemoteCallBody] RenewKitBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/kits/{id}/extend")]
    Task<CargoDryKitBffDto> ExtendKitAsync(
        long id,
        [AizenRemoteCallBody] ExtendKitBffRequest request,
        CancellationToken ct = default);

    // ── Stats Comparison & Warehouses ────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/stats/comparison")]
    Task<CargoDryStatsComparisonBffDto> GetStatsComparisonAsync(
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/warehouses")]
    Task<List<CargoDryWarehouseOptionBffDto>> GetWarehousesAsync(
        CancellationToken ct = default);

    // ── CSV Exports ───────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/kits/export")]
    Task<HttpResponseMessage> ExportKitsAsync(
        [Query] string? status    = null,
        [Query] string? search    = null,
        [Query] long?   vesselId  = null,
        [Query] string? batchCode = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/batches/export")]
    Task<HttpResponseMessage> ExportBatchesAsync(
        CancellationToken ct = default);

    // ── Reporting & Analytics ─────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/reports/usage")]
    Task<CargoDryKitUsageReportBffDto> GetUsageReportAsync(
        [Query] DateTimeOffset? dateFrom,
        [Query] DateTimeOffset? dateTo,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/reports/usage/export")]
    Task<HttpResponseMessage> ExportUsageReportAsync(
        [Query] string          format,
        [Query] DateTimeOffset? dateFrom,
        [Query] DateTimeOffset? dateTo,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/analytics")]
    Task<CargoDryAnalyticsRawBffDto> GetAnalyticsAsync(
        CancellationToken ct = default);

    // ── Consignment Agreements ───────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/consignment/agreements")]
    Task<ConsignmentAgreementPagedBffDto> GetConsignmentAgreementsPagedAsync(
        [Query] long?     providerProfileId,
        [Query] string?   productCode,
        [Query] int?      status,
        [Query] DateTime? dateFrom,
        [Query] DateTime? dateTo,
        [Query] string?   search,
        [Query] int       page,
        [Query] int       pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/consignment/agreements/{id}")]
    Task<ConsignmentAgreementBffDto?> GetConsignmentAgreementByIdAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/consignment/agreements/code/{code}")]
    Task<ConsignmentAgreementBffDto?> GetConsignmentAgreementByCodeAsync(
        string code,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/consignment/agreements/provider/{providerProfileId}/active")]
    Task<ConsignmentAgreementBffDto?> GetActiveConsignmentAgreementForProviderAsync(
        long providerProfileId,
        [Query] string productCode,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/consignment/agreements")]
    Task<ConsignmentAgreementBffDto> CreateConsignmentAgreementAsync(
        [AizenRemoteCallBody] CreateConsignmentAgreementBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/cargodry/admin/consignment/agreements/{id}")]
    Task<ConsignmentAgreementBffDto> UpdateConsignmentAgreementAsync(
        long id,
        [AizenRemoteCallBody] UpdateConsignmentAgreementBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/consignment/agreements/{id}/activate")]
    Task<ConsignmentAgreementBffDto> ActivateConsignmentAgreementAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/consignment/agreements/{id}/suspend")]
    Task<ConsignmentAgreementBffDto> SuspendConsignmentAgreementAsync(
        long id,
        [AizenRemoteCallBody] ConsignmentAgreementReasonBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/consignment/agreements/{id}/terminate")]
    Task<ConsignmentAgreementBffDto> TerminateConsignmentAgreementAsync(
        long id,
        [AizenRemoteCallBody] ConsignmentAgreementReasonBffRequest request,
        CancellationToken ct = default);

    // ── Provider Inventory ───────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/inventory")]
    Task<CargoDryProviderInventoryPagedBffDto> GetInventoryListAsync(
        [Query] long?   providerProfileId,
        [Query] string? productCode,
        [Query] int?    commercialModel,
        [Query] int?    salesChannel,
        [Query] bool?   hasAvailableStock,
        [Query] string? search,
        [Query] int     page,
        [Query] int     pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/inventory/provider/{providerProfileId}")]
    Task<CargoDryProviderInventoryDetailBffDto> GetInventoryDetailAsync(
        long providerProfileId,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/inventory/movements")]
    Task<CargoDryInventoryMovementPagedBffDto> GetInventoryMovementsAsync(
        [Query] long?   providerProfileId,
        [Query] string? productCode,
        [Query] string? batchCode,
        [Query] int?    movementType,
        [Query] DateTime? dateFrom,
        [Query] DateTime? dateTo,
        [Query] int     page,
        [Query] int     pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/inventory/preview")]
    Task<BatchAllocationPreviewBffDto> GetAllocationPreviewAsync(
        [Query] string batchCode,
        [Query] long   providerProfileId,
        [Query] int    commercialModel,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/inventory/allocate")]
    Task<AllocateBatchToProviderBffResultDto> AllocateBatchToProviderAsync(
        [AizenRemoteCallBody] AllocateBatchToProviderBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/inventory/adjust")]
    Task<CargoDryProviderInventoryBffDto> AdjustProviderInventoryAsync(
        [AizenRemoteCallBody] AdjustProviderInventoryBffRequest request,
        CancellationToken ct = default);

    // ── Commercial: Sales Attributions & Sell-Through Settlements ────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/sales-attributions")]
    Task<CargoDrySalesAttributionPagedBffDto> GetSalesAttributionsPagedAsync(
        [Query] long?     providerProfileId,
        [Query] string?   productCode,
        [Query] string?   batchCode,
        [Query] int?      salesChannel,
        [Query] int?      commercialModel,
        [Query] int?      status,
        [Query] long?     sellThroughSettlementId,
        [Query] DateTime? dateFrom,
        [Query] DateTime? dateTo,
        [Query] string?   search,
        [Query] int       page,
        [Query] int       pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/sales-attributions/{id}")]
    Task<CargoDrySalesAttributionBffDto?> GetSalesAttributionDetailAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/settlements")]
    Task<CargoDrySellThroughSettlementPagedBffDto> GetSellThroughSettlementsPagedAsync(
        [Query] long?     providerProfileId,
        [Query] long?     consignmentAgreementId,
        [Query] string?   productCode,
        [Query] int?      status,
        [Query] DateTime? periodFrom,
        [Query] DateTime? periodTo,
        [Query] string?   search,
        [Query] int       page,
        [Query] int       pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/settlements/{id}")]
    Task<CargoDrySellThroughSettlementBffDto?> GetSellThroughSettlementDetailAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/sales-attributions/{id}/resolve-financials")]
    Task<CargoDrySalesAttributionBffDto> ResolveAttributionFinancialsAsync(
        long id,
        [AizenRemoteCallBody] ResolveAttributionFinancialsBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/settlements/{id}/resolve-monthly")]
    Task<CargoDrySellThroughSettlementBffDto> ResolveMonthlySettlementAsync(
        long id,
        [AizenRemoteCallBody] ResolveMonthlySettlementBffRequest request,
        CancellationToken ct = default);

    // ── Phase 4B: Settlement Payment Preparation ─────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/settlements/{id}/payment-preparation-preview")]
    Task<CargoDrySettlementPaymentPreparationPreviewBffDto> GetSettlementPaymentPreparationPreviewAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/settlements/{id}/prepare-payment")]
    Task<PrepareCargoDrySettlementPaymentBffResponseDto> PrepareSettlementPaymentAsync(
        long id,
        [AizenRemoteCallBody] PrepareCargoDrySettlementPaymentBffRequest request,
        CancellationToken ct = default);

    // ── Phase 4C: Settlement Invoice Preparation ─────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/settlements/{id}/invoice-preparation-preview")]
    Task<CargoDrySettlementInvoicePreparationPreviewBffDto> GetSettlementInvoicePreparationPreviewAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/settlements/{id}/prepare-invoice")]
    Task<PrepareCargoDrySettlementInvoiceBffResponseDto> PrepareSettlementInvoiceAsync(
        long id,
        [AizenRemoteCallBody] PrepareCargoDrySettlementInvoiceBffRequest request,
        CancellationToken ct = default);

    // ── Phase 4D: Payout Lifecycle ────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/settlements/{id}/payout-execution-preview")]
    Task<CargoDrySettlementPayoutExecutionPreviewBffDto> GetSettlementPayoutExecutionPreviewAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/settlements/{id}/approve-payout")]
    Task<CargoDrySettlementPayoutLifecycleResponseBffDto> ApproveSettlementPayoutAsync(
        long id,
        [AizenRemoteCallBody] ApproveCargoDrySettlementPayoutBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/settlements/{id}/mark-payout-processing")]
    Task<CargoDrySettlementPayoutLifecycleResponseBffDto> MarkSettlementPayoutProcessingAsync(
        long id,
        [AizenRemoteCallBody] MarkCargoDrySettlementPayoutProcessingBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/settlements/{id}/complete-payout")]
    Task<CargoDrySettlementPayoutLifecycleResponseBffDto> CompleteSettlementPayoutAsync(
        long id,
        [AizenRemoteCallBody] CompleteCargoDrySettlementPayoutBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/settlements/{id}/fail-payout")]
    Task<CargoDrySettlementPayoutLifecycleResponseBffDto> FailSettlementPayoutAsync(
        long id,
        [AizenRemoteCallBody] FailCargoDrySettlementPayoutBffRequest request,
        CancellationToken ct = default);

    // ── Phase 5: Commercial Rule Resolution Preview ──────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/rules/resolve-preview")]
    Task<CargoDryCommercialRuleResolutionBffDto> GetRuleResolutionPreviewAsync(
        [Query] string              productCode,
        [Query] int                 salesChannel,
        [Query] int                 commercialModel,
        [Query] string              currencyCode,
        [Query] long?               providerProfileId      = null,
        [Query] decimal?            salePrice              = null,
        [Query] long?               consignmentAgreementId = null,
        [Query] decimal?            adminOverrideRate      = null,
        [Query] DateTime?           effectiveAtUtc         = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/sales-attributions/{id}/rule-resolution-preview")]
    Task<CargoDrySalesAttributionRuleResolutionPreviewBffDto> GetAttributionRuleResolutionPreviewAsync(
        long id,
        [Query] decimal? salePrice        = null,
        [Query] string?  currencyCode      = null,
        [Query] decimal? adminOverrideRate = null,
        CancellationToken ct = default);

    // ── Phase 6: Settlement Automation ──────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/settlement-automation/preview")]
    Task<CargoDrySettlementAutomationPreviewBffDto> GetSettlementAutomationPreviewAsync(
        [Query] int  targetYearMonth,
        [Query] bool autoPreparePayment = false,
        [Query] bool autoPrepareInvoice = false,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/settlement-automation/run")]
    Task<CargoDrySettlementAutomationRunBffDto> RunSettlementAutomationAsync(
        [AizenRemoteCallBody] RunSettlementAutomationBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/settlement-automation/runs")]
    Task<CargoDrySettlementAutomationRunsPagedBffDto> GetSettlementAutomationRunsAsync(
        [Query] int?      targetYearMonth   = null,
        [Query] int?      status            = null,
        [Query] int?      mode              = null,
        [Query] long?     triggeredByUserId = null,
        [Query] DateTime? fromUtc           = null,
        [Query] DateTime? toUtc             = null,
        [Query] int       page              = 1,
        [Query] int       pageSize          = 25,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/settlement-automation/runs/{id}")]
    Task<CargoDrySettlementAutomationRunBffDto> GetSettlementAutomationRunDetailAsync(
        long id,
        CancellationToken ct = default);

    // ── Phase 9: Kit Lifecycle History & Operational Alerts ──────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/kits/{id}/history")]
    Task<CargoDryKitLifecycleHistoryBffResponse> GetKitLifecycleHistoryAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/kits/lifecycle-events")]
    Task<CargoDryKitLifecycleEventsPagedBffResponse> GetKitLifecycleEventsPagedAsync(
        [Query] long?           kitId,
        [Query] string?         kitCode,
        [Query] string?         batchCode,
        [Query] string?         productCode,
        [Query] string?         eventType,
        [Query] long?           actorUserId,
        [Query] DateTimeOffset? dateFrom,
        [Query] DateTimeOffset? dateTo,
        [Query] int             page,
        [Query] int             pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/kits/operational-alerts")]
    Task<CargoDryOperationalAlertsBffResponse> GetOperationalAlertsAsync(
        [Query] int page,
        [Query] int pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/operational-overview")]
    Task<CargoDryOperationalOverviewBffDto> GetOperationalOverviewAsync(
        CancellationToken ct = default);

    // ── Phase 11: Renewal Billing & Notification Orchestration ───────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/renewals/candidates")]
    Task<List<CargoDryRenewalCandidateBffDto>> GetRenewalCandidatesAsync(
        [Query] int withinDays,
        [Query] int page,
        [Query] int pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/renewals")]
    Task<CargoDryRenewalPreparationBffDto> PrepareRenewalAsync(
        [AizenRemoteCallBody] PrepareRenewalBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/renewals")]
    Task<CargoDryRenewalPreparationsPagedBffResponse> GetRenewalPreparationsPagedAsync(
        [Query] long?   kitId,
        [Query] string? kitCode,
        [Query] string? productCode,
        [Query] long?   ownerUserId,
        [Query] long?   vesselId,
        [Query] int?    status,
        [Query] int?    notificationStatus,
        [Query] DateTimeOffset? preparedFrom,
        [Query] DateTimeOffset? preparedTo,
        [Query] int     page,
        [Query] int     pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/renewals/{id}")]
    Task<CargoDryRenewalPreparationBffDto?> GetRenewalPreparationDetailAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/renewals/{id}/invoice")]
    Task<CargoDryRenewalPreparationBffDto> PrepareRenewalInvoiceAsync(
        long id,
        [AizenRemoteCallBody] PrepareRenewalInvoiceBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/renewals/{id}/notification/prepare")]
    Task<CargoDryRenewalPreparationBffDto> PrepareRenewalNotificationAsync(
        long id,
        [AizenRemoteCallBody] PrepareRenewalNotificationBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/renewals/{id}/notification/dispatch")]
    Task<CargoDryRenewalPreparationBffDto> DispatchRenewalNotificationAsync(
        long id,
        [AizenRemoteCallBody] DispatchRenewalNotificationBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/renewals/{id}/complete")]
    Task<CargoDryRenewalPreparationBffDto> CompleteRenewalAsync(
        long id,
        [AizenRemoteCallBody] CompleteRenewalBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/renewals/{id}/cancel")]
    Task<CargoDryRenewalPreparationBffDto> CancelRenewalPreparationAsync(
        long id,
        [AizenRemoteCallBody] CancelRenewalPreparationBffRequest request,
        CancellationToken ct = default);

    // ── Onboarding (Public — no auth headers required) ───────────────────────

    [AizenRemoteCallPost("/api/v1/cargodry/public/validate")]
    Task<CargoDryValidationBffDto> ValidateKitAsync(
        [AizenRemoteCallBody] ValidateKitBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/kits/activate")]
    Task<CargoDryKitBffDto> ActivateKitAsync(
        [AizenRemoteCallBody] ActivateKitBffRequest request,
        CancellationToken ct = default);

    // ── Finance Reconciliation (Phase 15) ────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/finance/reconciliation/settlements")]
    Task<CargoDrySettlementReconciliationReportDto> GetSettlementReconciliationAsync(
        [Query] long?     providerProfileId = null,
        [Query] string?   productCode       = null,
        [Query] int?      status            = null,
        [Query] bool?     hasMismatches     = null,
        [Query] DateTime? dateFrom          = null,
        [Query] DateTime? dateTo            = null,
        [Query] int       page              = 1,
        [Query] int       pageSize          = 50,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/finance/reconciliation/renewals")]
    Task<CargoDryRenewalReconciliationReportDto> GetRenewalReconciliationAsync(
        [Query] string?           productCode        = null,
        [Query] long?             ownerUserId        = null,
        [Query] long?             vesselId           = null,
        [Query] int?              status             = null,
        [Query] int?              notificationStatus = null,
        [Query] bool?             hasMismatches      = null,
        [Query] DateTimeOffset?   dateFrom           = null,
        [Query] DateTimeOffset?   dateTo             = null,
        [Query] int               page               = 1,
        [Query] int               pageSize           = 50,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/finance/reports/commission-rule-usage")]
    Task<CargoDryCommissionRuleUsageReportDto> GetCommissionRuleUsageAsync(
        [Query] DateTime? dateFrom          = null,
        [Query] DateTime? dateTo            = null,
        [Query] long?     ruleId            = null,
        [Query] string?   productCode       = null,
        [Query] string?   salesChannel      = null,
        [Query] long?     providerProfileId = null,
        [Query] int       page              = 1,
        [Query] int       pageSize          = 50,
        CancellationToken ct = default);

    // ── Finance CSV Exports (Phase 16G) ──────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/finance/reconciliation/settlements/export")]
    Task<HttpResponseMessage> ExportSettlementReconciliationAsync(
        [Query] long?     providerProfileId = null,
        [Query] string?   productCode       = null,
        [Query] int?      status            = null,
        [Query] bool?     hasMismatches     = null,
        [Query] DateTime? dateFrom          = null,
        [Query] DateTime? dateTo            = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/finance/reconciliation/renewals/export")]
    Task<HttpResponseMessage> ExportRenewalReconciliationAsync(
        [Query] string?          productCode        = null,
        [Query] long?            ownerUserId        = null,
        [Query] long?            vesselId           = null,
        [Query] int?             status             = null,
        [Query] int?             notificationStatus = null,
        [Query] bool?            hasMismatches      = null,
        [Query] DateTimeOffset?  dateFrom           = null,
        [Query] DateTimeOffset?  dateTo             = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/finance/reports/commission-rule-usage/export")]
    Task<HttpResponseMessage> ExportCommissionRuleUsageAsync(
        [Query] DateTime? dateFrom          = null,
        [Query] DateTime? dateTo            = null,
        [Query] long?     ruleId            = null,
        [Query] string?   productCode       = null,
        [Query] string?   salesChannel      = null,
        [Query] long?     providerProfileId = null,
        CancellationToken ct = default);
}
