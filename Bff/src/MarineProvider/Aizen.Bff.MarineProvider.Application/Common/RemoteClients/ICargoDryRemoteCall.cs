using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

public interface ICargoDryRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/cargodry/provider/overview")]
    Task<AizenApiResponse<CargoDryOperationalOverviewDto>> GetOverview();

    [AizenRemoteCallGet("/api/v1/cargodry/provider/alerts")]
    Task<AizenApiResponse<CargoDryOperationalAlertsResponse>> GetAlerts(
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 25);

    [AizenRemoteCallGet("/api/v1/cargodry/provider/inventory")]
    Task<AizenApiResponse<CargoDryProviderInventoryPagedResultDto>> GetInventory(
        [Refit.Query] string? productCode = null, [Refit.Query] CargoDryCommercialModel? commercialModel = null,
        [Refit.Query] SalesChannel? salesChannel = null, [Refit.Query] bool? hasAvailableStock = null,
        [Refit.Query] string? search = null, [Refit.Query] int page = 1, [Refit.Query] int pageSize = 25);

    [AizenRemoteCallGet("/api/v1/cargodry/provider/inventory/movements")]
    Task<AizenApiResponse<CargoDryInventoryMovementPagedResultDto>> GetInventoryMovements(
        [Refit.Query] string? productCode = null, [Refit.Query] string? batchCode = null,
        [Refit.Query] InventoryMovementType? movementType = null,
        [Refit.Query] DateTime? dateFrom = null, [Refit.Query] DateTime? dateTo = null,
        [Refit.Query] int page = 1, [Refit.Query] int pageSize = 50);

    [AizenRemoteCallGet("/api/v1/cargodry/provider/renewals")]
    Task<AizenApiResponse<List<CargoDryRenewalCandidateDto>>> GetRenewals(
        [Refit.Query] int withinDays = 90, [Refit.Query] int page = 1, [Refit.Query] int pageSize = 50);

    [AizenRemoteCallGet("/api/v1/cargodry/provider/stock-requests")]
    Task<AizenApiResponse<CargoDryStockRequestPagedResultDto>> GetStockRequests(
        [Refit.Query] int? status = null, [Refit.Query] int page = 1, [Refit.Query] int pageSize = 25);

    [AizenRemoteCallPost("/api/v1/cargodry/provider/stock-requests")]
    Task<AizenApiResponse<CargoDryStockRequestDto>> CreateStockRequest(
        [AizenRemoteCallBody] CreateProviderStockRequestRequest body);

    [AizenRemoteCallPost("/api/v1/cargodry/provider/stock-requests/{id}/cancel")]
    Task<AizenApiResponse<CargoDryStockRequestDto>> CancelStockRequest(long id);

    [AizenRemoteCallGet("/api/v1/cargodry/provider/products")]
    Task<AizenApiResponse<List<CargoDryProductOptionDto>>> GetProducts();

    [AizenRemoteCallGet("/api/v1/cargodry/provider/catalog")]
    Task<AizenApiResponse<List<CargoDryProductDto>>> GetCatalog();
}
