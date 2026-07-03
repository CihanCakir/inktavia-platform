using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.RemoteCall.Abstraction;
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

    // ── Onboarding (Public — no auth headers required) ───────────────────────

    [AizenRemoteCallPost("/api/v1/cargodry/public/validate")]
    Task<CargoDryValidationBffDto> ValidateKitAsync(
        [AizenRemoteCallBody] ValidateKitBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/kits/activate")]
    Task<CargoDryKitBffDto> ActivateKitAsync(
        [AizenRemoteCallBody] ActivateKitBffRequest request,
        CancellationToken ct = default);
}
