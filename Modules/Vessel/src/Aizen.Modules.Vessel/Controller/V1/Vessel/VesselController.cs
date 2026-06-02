using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Application.Command.Vessel;
using Aizen.Modules.Vessel.Application.Query.Vessel;
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

    public VesselController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private long CurrentUserId =>
        long.Parse(ContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [ProducesResponseType(typeof(VesselDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselDto?>> Create([FromBody] CreateVesselRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselDto>(new CreateVesselCommand(req, CurrentUserId), ct);
        return SetResponse(result);
    }

    [HttpPut("{vesselId:long}")]
    [ProducesResponseType(typeof(VesselDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselDto?>> Update([FromRoute] long vesselId, [FromBody] UpdateVesselRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselDto>(new UpdateVesselCommand(vesselId, req, CurrentUserId), ct);
        return SetResponse(result);
    }

    [HttpGet("{vesselId:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VesselDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselDetailDto?>> GetDetail([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselDetailDto>(new GetVesselDetailQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpGet("code/{vesselCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VesselDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselDto?>> GetByCode([FromRoute] string vesselCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselDto>(new GetVesselByCodeQuery(vesselCode), ct);
        return SetResponse(result);
    }

    [HttpGet("current-user")]
    [ProducesResponseType(typeof(List<VesselListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<VesselListItemDto>?>> GetUserVessels(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<VesselListItemDto>>(new GetUserVesselsQuery(CurrentUserId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{vesselId:long}/archive")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Archive([FromRoute] long vesselId, [FromBody] ArchiveVesselRequest req, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ArchiveVesselCommand(vesselId, req, CurrentUserId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{vesselId:long}/restore")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Restore([FromRoute] long vesselId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new RestoreVesselCommand(vesselId, CurrentUserId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{vesselId:long}/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> UpdateStatus([FromRoute] long vesselId, [FromBody] UpdateVesselStatusRequest req, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new UpdateVesselStatusCommand(vesselId, req, CurrentUserId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{vesselId:long}/visibility")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> UpdateVisibility([FromRoute] long vesselId, [FromBody] VesselVisibility visibility, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new UpdateVesselVisibilityCommand(vesselId, visibility, CurrentUserId), ct);
        return SetResponse<object>(new { success = true });
    }
}
