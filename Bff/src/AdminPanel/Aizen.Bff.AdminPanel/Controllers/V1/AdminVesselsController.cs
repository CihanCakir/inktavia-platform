using Aizen.Bff.AdminPanel.Application.AdminVessels.Command;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Vessels")]
[Authorize(Roles = "Admin")]
public sealed class VesselsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("vessels")]
    [ProducesResponseType(typeof(AdminVesselOverviewResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselOverviewResponse>> GetVessels(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isArchived = null,
        CancellationToken ct = default)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminVesselOverviewQuery(auth, userToken, pageIndex, pageSize, searchTerm, isArchived), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/{vesselId:long}/detail")]
    [ProducesResponseType(typeof(AdminVesselDocumentsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselDocumentsResponse>> GetVesselDetail(
        long vesselId, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminVesselDocumentsQuery(vesselId, auth, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("vessels/{vesselId:long}/archive")]
    [ProducesResponseType(typeof(ArchiveVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ArchiveVesselResponse>> ArchiveVessel(
        long vesselId, [FromBody] ArchiveVesselRequest request, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new ArchiveVesselCommand(vesselId, request, auth, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("vessels/{vesselId:long}/restore")]
    [ProducesResponseType(typeof(RestoreVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RestoreVesselResponse>> RestoreVessel(
        long vesselId, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new RestoreVesselCommand(vesselId, auth, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("vessels/{vesselId:long}/status")]
    [ProducesResponseType(typeof(UpdateVesselStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselStatusResponse>> UpdateVesselStatus(
        long vesselId, [FromBody] UpdateVesselStatusRequest request, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new UpdateVesselStatusCommand(vesselId, request, auth, userToken), ct);
        return SetResponse(result);
    }

    [HttpDelete("vessels/{vesselId:long}/documents/{documentId:long}")]
    [ProducesResponseType(typeof(RemoveVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RemoveVesselDocumentResponse>> RemoveVesselDocument(
        long vesselId, long documentId, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new RemoveVesselDocumentCommand(vesselId, documentId, auth, userToken), ct);
        return SetResponse(result);
    }
}
