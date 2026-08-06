using Aizen.Bff.AdminPanel.Application.Vessels.Command;
using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Bff.AdminPanel.Application.Vessels.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Status;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Vessels")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class VesselsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("vessels")]
    [ProducesResponseType(typeof(AdminVesselListBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselListBffResponse>> GetVessels(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isArchived = null,
        [FromQuery] int[]? assetTypes = null,
        [FromQuery] int[]? ownershipStatuses = null,
        [FromQuery] int[]? operationalStatuses = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetVesselListBffQuery(pageIndex, pageSize, searchTerm, isArchived, assetTypes, ownershipStatuses, operationalStatuses), ct);
        return SetResponse(result);
    }

    // Literal routes must be declared before parameterized routes to avoid ambiguity.

    [HttpGet("vessels/register")]
    [ProducesResponseType(typeof(AdminVesselRegisterBootstrapBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselRegisterBootstrapBffResponse>> GetVesselRegisterBootstrap(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetVesselRegisterBootstrapBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpPost("vessels/register")]
    [ProducesResponseType(typeof(CreateVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateVesselResponse>> RegisterVessel(
        [FromBody] RegisterAdminVesselBffRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new RegisterVesselBffCommand(request), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/form-options")]
    [ProducesResponseType(typeof(AdminVesselFormOptionsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselFormOptionsResponse>> GetFormOptions(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetVesselFormOptionsBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/{vesselId:long}/detail")]
    [ProducesResponseType(typeof(AdminVesselDetailBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselDetailBffResponse>> GetVesselDetail(
        long vesselId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetVesselDetailBffQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPatch("vessels/{vesselId:long}/archive")]
    [ProducesResponseType(typeof(ArchiveVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ArchiveVesselResponse>> ArchiveVessel(
        long vesselId, [FromBody] ArchiveVesselRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ArchiveVesselBffCommand(vesselId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("vessels/{vesselId:long}/restore")]
    [ProducesResponseType(typeof(RestoreVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RestoreVesselResponse>> RestoreVessel(
        long vesselId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RestoreVesselBffCommand(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPatch("vessels/{vesselId:long}/status")]
    [ProducesResponseType(typeof(UpdateVesselStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselStatusResponse>> UpdateVesselStatus(
        long vesselId, [FromBody] UpdateVesselStatusRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateVesselStatusBffCommand(vesselId, request), ct);
        return SetResponse(result);
    }

    [HttpDelete("vessels/{vesselId:long}/documents/{documentId:long}")]
    [ProducesResponseType(typeof(RemoveVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RemoveVesselDocumentResponse>> RemoveVesselDocument(
        long vesselId, long documentId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RemoveVesselDocumentBffCommand(vesselId, documentId), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/{vesselId:long}")]
    [ProducesResponseType(typeof(GetVesselDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselDetailResponse>> GetVesselById(
        long vesselId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetVesselByIdBffQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/{vesselId:long}/documents")]
    [ProducesResponseType(typeof(AdminVesselDocumentsBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselDocumentsBffResponse>> GetVesselDocuments(
        long vesselId,
        [FromQuery] string? statusFilter = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetVesselDocumentsBffQuery(vesselId, statusFilter), ct);
        return SetResponse(result);
    }

    [HttpPut("vessels/{vesselId:long}")]
    [ProducesResponseType(typeof(UpdateVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselResponse>> UpdateVessel(
        long vesselId, [FromBody] UpdateVesselRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new UpdateVesselBffCommand(vesselId, request), ct);
        return SetResponse(result);
    }

    [HttpGet("vessels/{vesselId:long}/media")]
    [ProducesResponseType(typeof(AdminVesselMediaBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselMediaBffResponse>> GetVesselMedia(
        long vesselId,
        [FromQuery] string? mediaType = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetVesselMediaBffQuery(vesselId, mediaType, pageIndex, pageSize), ct);
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
        var result = await _cqrs.ProcessAsync(
            new GetVesselStatusHistoryBffQuery(vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }
}
