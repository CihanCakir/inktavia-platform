using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Modules.Vessel.Application.Command.Vessel;
using Aizen.Modules.Vessel.Application.Query.Vessel;
using Aizen.Modules.Vessel.Application.Query.Vessel.GetVesselSummaries;
using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.Vessel.Controller.V1.Vessel;

[ApiController]
[Route("api/v1/vessels")]
[Tags("Vessel")]
[Authorize]
[DocumentationInfo("Vessel endpoints", "Create, update, archive and query vessel resources.")]
public sealed class VesselController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IAizenInfoAccessor _info;

    public VesselController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs, IAizenInfoAccessor info)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _info = info;
    }

    // Resolve the caller's user id the same way the module's command handlers do — from the InfoAccessor,
    // which is populated by both real user tokens AND the trusted-BFF identity assertion (BFF service-token
    // callers carry no numeric NameIdentifier claim). Falls back to a numeric NameIdentifier claim if present.
    private long CurrentUserId
    {
        get
        {
            var fromInfo = _info.UserInfoAccessor.UserInfo.UserId;
            if (fromInfo > 0) return fromInfo;
            return long.TryParse(ContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateVesselResponse?>> Create([FromBody] CreateVesselRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CreateVesselResponse>(new CreateVesselCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{vesselId:long}")]
    [ProducesResponseType(typeof(UpdateVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselResponse?>> Update([FromRoute] long vesselId, [FromBody] UpdateVesselRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateVesselResponse>(new UpdateVesselCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpGet("{vesselId:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetVesselDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselDetailResponse?>> GetDetail([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetVesselDetailResponse>(new GetVesselDetailQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpGet("code/{vesselCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetVesselByCodeResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselByCodeResponse?>> GetByCode([FromRoute] string vesselCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetVesselByCodeResponse>(new GetVesselByCodeQuery(vesselCode), ct);
        return SetResponse(result);
    }

    [HttpGet("current-user")]
    [ProducesResponseType(typeof(GetUserVesselsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetUserVesselsResponse?>> GetUserVessels(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetUserVesselsResponse>(new GetUserVesselsQuery(CurrentUserId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpPatch("{vesselId:long}/archive")]
    [ProducesResponseType(typeof(ArchiveVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ArchiveVesselResponse?>> Archive([FromRoute] long vesselId, [FromBody] ArchiveVesselRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ArchiveVesselResponse>(new ArchiveVesselCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("{vesselId:long}/restore")]
    [ProducesResponseType(typeof(RestoreVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RestoreVesselResponse?>> Restore([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RestoreVesselResponse>(new RestoreVesselCommand(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{vesselId:long}/status")]
    [ProducesResponseType(typeof(UpdateVesselStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselStatusResponse?>> UpdateStatus([FromRoute] long vesselId, [FromBody] UpdateVesselStatusRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateVesselStatusResponse>(new UpdateVesselStatusCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpGet("summary")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetVesselSummariesResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselSummariesResponse?>> GetSummaries(
        [FromQuery(Name = "ids")] string idsParam,
        CancellationToken ct = default)
    {
        var ids = string.IsNullOrWhiteSpace(idsParam)
            ? Array.Empty<long>()
            : idsParam.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => long.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

        var result = await _cqrs.ProcessAsync<GetVesselSummariesResponse>(
            new GetVesselSummariesQuery(ids), ct);
        return SetResponse(result);
    }

    [HttpPatch("{vesselId:long}/visibility")]
    [ProducesResponseType(typeof(UpdateVesselVisibilityResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselVisibilityResponse?>> UpdateVisibility([FromRoute] long vesselId, [FromBody] VesselVisibility visibility, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateVesselVisibilityResponse>(new UpdateVesselVisibilityCommand(vesselId, visibility), ct);
        return SetResponse(result);
    }
}
