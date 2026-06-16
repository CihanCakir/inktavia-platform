using Aizen.Bff.AdminPanel.Application.AdminVessels.Command;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Media;
using Aizen.Modules.Vessel.Abstraction.Response.Status;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Vessels")]
[Authorize]
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
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminVesselOverviewQuery(userToken, pageIndex, pageSize, searchTerm, isArchived), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/{vesselId:long}/detail")]
    [ProducesResponseType(typeof(AdminVesselDocumentsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselDocumentsResponse>> GetVesselDetail(
        long vesselId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminVesselDocumentsQuery(vesselId, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("vessels/{vesselId:long}/archive")]
    [ProducesResponseType(typeof(ArchiveVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ArchiveVesselResponse>> ArchiveVessel(
        long vesselId, [FromBody] ArchiveVesselRequest request, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new ArchiveVesselCommand(vesselId, request, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("vessels/{vesselId:long}/restore")]
    [ProducesResponseType(typeof(RestoreVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RestoreVesselResponse>> RestoreVessel(
        long vesselId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new RestoreVesselCommand(vesselId, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("vessels/{vesselId:long}/status")]
    [ProducesResponseType(typeof(UpdateVesselStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselStatusResponse>> UpdateVesselStatus(
        long vesselId, [FromBody] UpdateVesselStatusRequest request, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new UpdateVesselStatusCommand(vesselId, request, userToken), ct);
        return SetResponse(result);
    }

    [HttpDelete("vessels/{vesselId:long}/documents/{documentId:long}")]
    [ProducesResponseType(typeof(RemoveVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RemoveVesselDocumentResponse>> RemoveVesselDocument(
        long vesselId, long documentId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new RemoveVesselDocumentCommand(vesselId, documentId, userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/{vesselId:long}")]
    [ProducesResponseType(typeof(GetVesselDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselDetailResponse>> GetVesselById(
        long vesselId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetAdminVesselByIdQuery(vesselId, userToken), ct);
        return SetResponse(result);
    }

    [HttpPut("vessels/{vesselId:long}")]
    [ProducesResponseType(typeof(UpdateVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselResponse>> UpdateVessel(
        long vesselId, [FromBody] UpdateVesselRequest request, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new UpdateVesselCommand(vesselId, request, userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/form-options")]
    [ProducesResponseType(typeof(AdminVesselFormOptionsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselFormOptionsResponse>> GetFormOptions(CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetAdminVesselFormOptionsQuery(userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/{vesselId:long}/media")]
    [ProducesResponseType(typeof(GetVesselMediaResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselMediaResponse>> GetVesselMedia(
        long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminVesselMediaQuery(vesselId, userToken, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/{vesselId:long}/status-history")]
    [ProducesResponseType(typeof(GetVesselStatusHistoryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselStatusHistoryResponse>> GetVesselStatusHistory(
        long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminVesselStatusHistoryQuery(vesselId, userToken, pageIndex, pageSize), ct);
        return SetResponse(result);
    }
}
