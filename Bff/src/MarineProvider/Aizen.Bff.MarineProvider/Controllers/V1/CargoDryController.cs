using Aizen.Bff.MarineProvider.Application.CargoDry;
using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/cargodry")]
[Tags("Provider - CargoDry")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class CargoDryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public CargoDryController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("overview")]
    [ProducesResponseType(typeof(CargoDryOperationalOverviewDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryOperationalOverviewDto?>> GetOverview(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryOverviewBffQuery(), ct));

    [HttpGet("alerts")]
    [ProducesResponseType(typeof(CargoDryOperationalAlertsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryOperationalAlertsResponse?>> GetAlerts([FromQuery] int take = 5, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryAlertsBffQuery { Take = take }, ct));

    [HttpGet("inventory")]
    [ProducesResponseType(typeof(CargoDryProviderInventoryPagedResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProviderInventoryPagedResultDto?>> GetInventory(
        [FromQuery] string? productCode = null, [FromQuery] CargoDryCommercialModel? commercialModel = null,
        [FromQuery] SalesChannel? salesChannel = null, [FromQuery] bool? hasAvailableStock = null,
        [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryInventoryBffQuery { ProductCode = productCode, CommercialModel = commercialModel, SalesChannel = salesChannel, HasAvailableStock = hasAvailableStock, Search = search, Page = page, PageSize = pageSize }, ct));

    [HttpGet("inventory/movements")]
    [ProducesResponseType(typeof(CargoDryInventoryMovementPagedResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryInventoryMovementPagedResultDto?>> GetInventoryMovements(
        [FromQuery] string? productCode = null, [FromQuery] string? batchCode = null, [FromQuery] InventoryMovementType? movementType = null,
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryInventoryMovementsBffQuery { ProductCode = productCode, BatchCode = batchCode, MovementType = movementType, DateFrom = dateFrom, DateTo = dateTo, Page = page, PageSize = pageSize }, ct));

    [HttpGet("renewals")]
    [ProducesResponseType(typeof(List<CargoDryRenewalCandidateDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryRenewalCandidateDto>?>> GetRenewals([FromQuery] int withinDays = 90, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryRenewalsBffQuery { WithinDays = withinDays, Page = page, PageSize = pageSize }, ct));

    [HttpGet("stock-requests")]
    [ProducesResponseType(typeof(CargoDryStockRequestPagedResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryStockRequestPagedResultDto?>> GetStockRequests([FromQuery] int? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryStockRequestsBffQuery { Status = status, Page = page, PageSize = pageSize }, ct));

    [HttpPost("stock-requests")]
    [ProducesResponseType(typeof(CargoDryStockRequestDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryStockRequestDto?>> CreateStockRequest([FromBody] CreateProviderStockRequestRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new CreateCargoDryStockRequestBffCommand { ProductCode = body.ProductCode, RequestedQuantity = body.RequestedQuantity, Note = body.Note }, ct));

    [HttpPost("stock-requests/{id:long}/cancel")]
    [ProducesResponseType(typeof(CargoDryStockRequestDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryStockRequestDto?>> CancelStockRequest([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new CancelCargoDryStockRequestBffCommand { Id = id }, ct));

    [HttpGet("products")]
    [ProducesResponseType(typeof(List<CargoDryProductOptionDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryProductOptionDto>?>> GetProducts(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryProductsBffQuery(), ct));

    [HttpGet("catalog")]
    [ProducesResponseType(typeof(List<CargoDryProductDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryProductDto>?>> GetCatalog(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryCatalogBffQuery(), ct));

    [HttpGet("earnings")]
    [ProducesResponseType(typeof(CargoDryProviderEarningsDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProviderEarningsDto?>> GetEarnings(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryEarningsBffQuery(), ct));

    [HttpGet("earnings/trend")]
    [ProducesResponseType(typeof(List<CargoDryEarningsTrendPointDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryEarningsTrendPointDto>?>> GetEarningsTrend(
        [FromQuery] int months = 6, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryTrendBffQuery { Months = months }, ct));

    [HttpGet("products/performance")]
    [ProducesResponseType(typeof(List<CargoDryProductPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryProductPerformanceDto>?>> GetProductPerformance(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryProductPerformanceBffQuery(), ct));

    [HttpGet("tier")]
    [ProducesResponseType(typeof(CargoDryProviderTierDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProviderTierDto?>> GetTier(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryProviderTierBffQuery(), ct));

    [HttpGet("momentum")]
    [ProducesResponseType(typeof(CargoDryProviderMomentumDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProviderMomentumDto?>> GetMomentum(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryProviderMomentumBffQuery(), ct));
}
