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
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderEarnings;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderCommissionTrend;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderProductPerformance;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderParticipation;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderTier;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderMomentum;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderSettlements;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderPayoutSummary;
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

    /// <summary>
    /// Is this provider in the CargoDry programme? Read by the MarineProvider BFF to publish a `CargoDry`
    /// capability on GET /me/status (PROV-MVP-002/003). Deliberately the cheapest call on this controller — it
    /// sits on the workspace-entry path.
    /// </summary>
    [HttpGet("participation")]
    public async Task<AizenApiResponse<CargoDryProviderParticipationDto?>> GetParticipation(
        CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        return SetResponse(await _cqrs.ProcessAsync(
            new GetCargoDryProviderParticipationQuery { ProviderProfileId = pid }, ct));
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

    [HttpGet("earnings")]
    public async Task<AizenApiResponse<CargoDryProviderEarningsDto?>> GetEarnings(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryProviderEarningsDto>(
            new GetCargoDryProviderEarningsQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }

    [HttpGet("earnings/trend")]
    public async Task<AizenApiResponse<List<CargoDryEarningsTrendPointDto>?>> GetEarningsTrend(
        [FromQuery] int months = 6, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<List<CargoDryEarningsTrendPointDto>>(
            new GetCargoDryProviderCommissionTrendQuery { ProviderProfileId = pid, Months = months }, ct);
        return SetResponse(result);
    }

    [HttpGet("products/performance")]
    public async Task<AizenApiResponse<List<CargoDryProductPerformanceDto>?>> GetProductPerformance(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<List<CargoDryProductPerformanceDto>>(
            new GetCargoDryProviderProductPerformanceQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }

    [HttpGet("tier")]
    public async Task<AizenApiResponse<CargoDryProviderTierDto?>> GetTier(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryProviderTierDto>(
            new GetCargoDryProviderTierQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }

    [HttpGet("momentum")]
    public async Task<AizenApiResponse<CargoDryProviderMomentumDto?>> GetMomentum(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryProviderMomentumDto>(
            new GetCargoDryProviderMomentumQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }

    [HttpGet("settlements")]
    public async Task<AizenApiResponse<CargoDryProviderSettlementPagedResultDto?>> GetSettlements(
        [FromQuery] int? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryProviderSettlementPagedResultDto>(
            new GetCargoDryProviderSettlementsQuery
            {
                ProviderProfileId = pid,
                Status = status,
                Page = page,
                PageSize = pageSize,
                From = from.HasValue ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc) : null,
                To   = to.HasValue   ? DateTime.SpecifyKind(to.Value,   DateTimeKind.Utc) : null,
            }, ct);
        return SetResponse(result);
    }

    [HttpGet("settlements/summary")]
    public async Task<AizenApiResponse<CargoDryProviderPayoutSummaryDto?>> GetPayoutSummary(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<CargoDryProviderPayoutSummaryDto>(
            new GetCargoDryProviderPayoutSummaryQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }
}

public sealed class CancelStockRequestBody
{
    public string? Reason { get; set; }
}
