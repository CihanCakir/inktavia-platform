using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Status;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Modules.Vessel.Application.Command.Vessel;
using Aizen.Modules.Vessel.Application.Query.Vessel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Vessel.Controller.V1.Admin.Vessel;

[ApiController]
[Route("api/v1/admin/vessels")]
[Tags("Admin - Vessel")]
[Authorize(Roles = RoleNames.Admin)]
[DocumentationInfo("Admin vessel endpoints", "Admin-only vessel management and reporting.")]
public sealed class VesselAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetAllVesselsAdminResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetAllVesselsAdminResponse?>> GetAll(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isArchived = null,
        [FromQuery] int[]? assetTypes = null,
        [FromQuery] int[]? ownershipStatuses = null,
        [FromQuery] int[]? operationalStatuses = null,
        [FromQuery] long? ownerUserId = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetAllVesselsAdminResponse>(
            new GetAllVesselsAdminQuery(pageIndex, pageSize, searchTerm, isArchived, assetTypes, ownershipStatuses, operationalStatuses, ownerUserId), ct);
        return SetResponse(result);
    }

    [HttpGet("counts-by-owner")]
    [ProducesResponseType(typeof(List<VesselCountByOwnerDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<VesselCountByOwnerDto>?>> GetCountsByOwner(
        [FromQuery] long[] userIds,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<VesselCountByOwnerDto>>(
            new GetVesselCountsByOwnerUserIdsQuery(userIds ?? Array.Empty<long>()), ct);
        return SetResponse(result);
    }

    [HttpGet("names-by-ids")]
    [ProducesResponseType(typeof(List<VesselNameDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<VesselNameDto>?>> GetNamesByIds(
        [FromQuery] long[] vesselIds,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<VesselNameDto>>(
            new GetVesselNamesByIdsQuery(vesselIds ?? Array.Empty<long>()), ct);
        return SetResponse(result);
    }

    [HttpGet("stats/status-counts")]
    [ProducesResponseType(typeof(List<VesselStatusCountDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<VesselStatusCountDto>?>> GetStatusCounts(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<VesselStatusCountDto>>(
            new GetVesselStatusCountsQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("status-history/by-owner")]
    [ProducesResponseType(typeof(GetVesselStatusHistoryByOwnerResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselStatusHistoryByOwnerResponse?>> GetStatusHistoryByOwner(
        [FromQuery] long ownerUserId,
        [FromQuery] int pageSize = 200,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetVesselStatusHistoryByOwnerResponse>(
            new GetVesselStatusHistoryByOwnerQuery(ownerUserId, pageSize), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateVesselResponse?>> Create(
        [FromBody] CreateAdminVesselRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CreateVesselResponse>(new CreateAdminVesselCommand(req), ct);
        return SetResponse(result);
    }
}
