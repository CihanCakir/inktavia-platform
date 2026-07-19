using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOperationalAlerts;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOperationalOverview;
using Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryList;
using Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryMovements;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalCandidates;
using Aizen.Modules.CargoDry.Application.Queries.GetProviderStockRequests;
using Aizen.Modules.CargoDry.Application.Commands.CreateProviderStockRequest;
using Aizen.Modules.CargoDry.Application.Commands.CancelProviderStockRequest;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

/// <summary>
/// Provider-scoped CargoDry surface. Identity from the BFF assertion (X-Aizen-Provider-Profile-Id).
/// </summary>
[ApiController]
[Route("api/v1/cargodry/provider")]
[Tags("CargoDry - Provider")]
[Authorize]
public sealed class CargoDryProviderController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IAizenInfoAccessor _info;

    public CargoDryProviderController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs, IAizenInfoAccessor info)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _info = info;
    }

    private long ResolveProviderProfileId()
    {
        var pid = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (pid <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");
        return pid;
    }

    [HttpGet("overview")]
    public async Task<AizenApiResponse<CargoDryOperationalOverviewDto?>> GetOverview(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryOperationalOverviewDto>(
            new GetCargoDryOperationalOverviewQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }

    [HttpGet("alerts")]
    public async Task<AizenApiResponse<CargoDryOperationalAlertsResponse?>> GetAlerts(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryOperationalAlertsResponse>(
            new GetCargoDryOperationalAlertsQuery { ProviderProfileId = pid, Page = page, PageSize = pageSize }, ct);
        return SetResponse(result);
    }

    [HttpGet("inventory")]
    public async Task<AizenApiResponse<CargoDryProviderInventoryPagedResultDto?>> GetInventory(
        [FromQuery] string? productCode = null, [FromQuery] CargoDryCommercialModel? commercialModel = null,
        [FromQuery] SalesChannel? salesChannel = null, [FromQuery] bool? hasAvailableStock = null,
        [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryProviderInventoryPagedResultDto>(
            new GetProviderInventoryListQuery
            {
                ProviderProfileId = pid, ProductCode = productCode, CommercialModel = commercialModel,
                SalesChannel = salesChannel, HasAvailableStock = hasAvailableStock, Search = search,
                Page = page, PageSize = pageSize
            }, ct);
        return SetResponse(result);
    }

    [HttpGet("inventory/movements")]
    public async Task<AizenApiResponse<CargoDryInventoryMovementPagedResultDto?>> GetInventoryMovements(
        [FromQuery] string? productCode = null, [FromQuery] string? batchCode = null,
        [FromQuery] InventoryMovementType? movementType = null,
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryInventoryMovementPagedResultDto>(
            new GetProviderInventoryMovementsQuery
            {
                ProviderProfileId = pid, ProductCode = productCode, BatchCode = batchCode,
                MovementType = movementType, DateFrom = dateFrom, DateTo = dateTo,
                Page = page, PageSize = pageSize
            }, ct);
        return SetResponse(result);
    }

    [HttpGet("renewals")]
    public async Task<AizenApiResponse<List<CargoDryRenewalCandidateDto>?>> GetRenewalCandidates(
        [FromQuery] int withinDays = 90, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<List<CargoDryRenewalCandidateDto>>(
            new GetCargoDryRenewalCandidatesQuery
            {
                ProviderProfileId = pid, WithinDays = withinDays, Page = page, PageSize = pageSize
            }, ct);
        return SetResponse(result);
    }

    [HttpGet("stock-requests")]
    public async Task<AizenApiResponse<CargoDryStockRequestPagedResultDto?>> GetStockRequests(
        [FromQuery] Abstraction.Enum.CargoDryStockRequestStatus? status = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryStockRequestPagedResultDto>(
            new GetProviderStockRequestsQuery { ProviderProfileId = pid, Status = status, Page = page, PageSize = pageSize }, ct);
        return SetResponse(result);
    }

    [HttpPost("stock-requests")]
    public async Task<AizenApiResponse<CargoDryStockRequestDto?>> CreateStockRequest(
        [FromBody] CreateProviderStockRequestRequest body, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryStockRequestDto>(
            new CreateProviderStockRequestCommand
            {
                ProviderProfileId = pid, ProductCode = body.ProductCode,
                RequestedQuantity = body.RequestedQuantity, Note = body.Note
            }, ct);
        return SetResponse(result);
    }

    [HttpPost("stock-requests/{id:long}/cancel")]
    public async Task<IActionResult> CancelStockRequest([FromRoute] long id, [FromBody] CancelStockRequestBody? body, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        await _cqrs.ProcessAsync<bool>(
            new CancelProviderStockRequestCommand { ProviderProfileId = pid, RequestId = id, Reason = body?.Reason }, ct);
        return Ok(new { Header = new { IsSuccess = true } });
    }

    [HttpGet("products")]
    public async Task<AizenApiResponse<List<CargoDryProductOptionDto>?>> GetProducts(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var inventoryResult = await _cqrs.ProcessAsync<CargoDryProviderInventoryPagedResultDto>(
            new GetProviderInventoryListQuery { ProviderProfileId = pid, Page = 1, PageSize = 100 }, ct);
        var products = inventoryResult?.Items
            .Select(i => new CargoDryProductOptionDto { ProductCode = i.ProductCode })
            .DistinctBy(p => p.ProductCode)
            .ToList() ?? new List<CargoDryProductOptionDto>();
        return SetResponse(products);
    }

    [HttpGet("catalog")]
    public async Task<AizenApiResponse<List<CargoDryProductDto>?>> GetCatalog(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<CargoDryProductDto>>(new GetCargoDryProductListQuery(), ct);
        return SetResponse(result);
    }
}

public sealed class CancelStockRequestBody
{
    public string? Reason { get; set; }
}
