using Aizen.Bff.AdminPanel.Application.Vessels.Command;
using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Bff.AdminPanel.Application.Vessels.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Enum;
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
[Route("api/v1/admin-panel/vessels")]
[Tags("Admin Panel - Vessels")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class VesselsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet]
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

    [HttpGet("register")]
    [ProducesResponseType(typeof(AdminVesselRegisterBootstrapBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselRegisterBootstrapBffResponse>> GetVesselRegisterBootstrap(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetVesselRegisterBootstrapBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(CreateVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateVesselResponse>> RegisterVessel(
        [FromBody] RegisterAdminVesselBffRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new RegisterVesselBffCommand(request), ct);
        return SetResponse(result);
    }

    [HttpGet("form-options")]
    [ProducesResponseType(typeof(AdminVesselFormOptionsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselFormOptionsResponse>> GetFormOptions(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetVesselFormOptionsBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("{vesselId:long}/detail")]
    [ProducesResponseType(typeof(AdminVesselDetailBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselDetailBffResponse>> GetVesselDetail(
        long vesselId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetVesselDetailBffQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{vesselId:long}/archive")]
    [ProducesResponseType(typeof(ArchiveVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ArchiveVesselResponse>> ArchiveVessel(
        long vesselId, [FromBody] ArchiveVesselRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ArchiveVesselBffCommand(vesselId, request), ct);
        return SetResponse(result);
    }

    [HttpPatch("{vesselId:long}/restore")]
    [ProducesResponseType(typeof(RestoreVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RestoreVesselResponse>> RestoreVessel(
        long vesselId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RestoreVesselBffCommand(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{vesselId:long}/status")]
    [ProducesResponseType(typeof(UpdateVesselStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselStatusResponse>> UpdateVesselStatus(
        long vesselId, [FromBody] UpdateVesselStatusRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateVesselStatusBffCommand(vesselId, request), ct);
        return SetResponse(result);
    }

    [HttpDelete("{vesselId:long}/documents/{documentId:long}")]
    [ProducesResponseType(typeof(RemoveVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RemoveVesselDocumentResponse>> RemoveVesselDocument(
        long vesselId, long documentId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RemoveVesselDocumentBffCommand(vesselId, documentId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{vesselId:long}/documents/{documentId:long}/approve")]
    [ProducesResponseType(typeof(ApproveVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ApproveVesselDocumentResponse>> ApproveVesselDocument(
        long vesselId, long documentId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ApproveVesselDocumentBffCommand(vesselId, documentId), ct);
        return SetResponse(result);
    }

    // ── Document upload / replace (B3/B4): presigned-PUT two-step ──────────────────────────────

    [HttpPost("{vesselId:long}/documents/upload-url")]
    [ProducesResponseType(typeof(VesselFileUploadUrlBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselFileUploadUrlBffResponse>> RequestDocumentUploadUrl(
        long vesselId, [FromBody] VesselFileUploadUrlRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RequestVesselFileUploadUrlBffCommand(vesselId, request.FileName, request.ContentType, request.FileSizeBytes), ct);
        return SetResponse(result);
    }

    [HttpPost("{vesselId:long}/documents")]
    [ProducesResponseType(typeof(AddVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddVesselDocumentResponse>> RegisterDocument(
        long vesselId, [FromBody] RegisterVesselDocumentRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RegisterVesselDocumentBffCommand(
                vesselId, request.FileId, request.UploadSessionCode,
                request.DocumentTypeCode, request.DocumentName, request.ExpiresAt, request.Notes), ct);
        return SetResponse(result);
    }

    [HttpPost("{vesselId:long}/documents/{documentId:long}/versions")]
    [ProducesResponseType(typeof(UpdateVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselDocumentResponse>> ReplaceDocument(
        long vesselId, long documentId, [FromBody] RegisterVesselDocumentRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ReplaceVesselDocumentBffCommand(
                vesselId, documentId, request.FileId, request.UploadSessionCode,
                request.DocumentTypeCode, request.DocumentName, request.ExpiresAt, request.Notes), ct);
        return SetResponse(result);
    }

    // ── Media upload (B3): presigned-PUT two-step ─────────────────────────────────────────────

    [HttpPost("{vesselId:long}/media/upload-url")]
    [ProducesResponseType(typeof(VesselFileUploadUrlBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselFileUploadUrlBffResponse>> RequestMediaUploadUrl(
        long vesselId, [FromBody] VesselFileUploadUrlRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RequestVesselFileUploadUrlBffCommand(vesselId, request.FileName, request.ContentType, request.FileSizeBytes), ct);
        return SetResponse(result);
    }

    [HttpPost("{vesselId:long}/media")]
    [ProducesResponseType(typeof(AddVesselMediaResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddVesselMediaResponse>> RegisterMedia(
        long vesselId, [FromBody] RegisterVesselMediaRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RegisterVesselMediaBffCommand(
                vesselId, request.FileId, request.UploadSessionCode,
                request.MediaType, request.IsCover, request.SortOrder), ct);
        return SetResponse(result);
    }

    [HttpGet("{vesselId:long}")]
    [ProducesResponseType(typeof(AdminVesselByIdBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminVesselByIdBffResponse>> GetVesselById(
        long vesselId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetVesselByIdBffQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpGet("{vesselId:long}/documents")]
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

    [HttpPut("{vesselId:long}")]
    [ProducesResponseType(typeof(UpdateVesselResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselResponse>> UpdateVessel(
        long vesselId, [FromBody] UpdateVesselRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new UpdateVesselBffCommand(vesselId, request), ct);
        return SetResponse(result);
    }

    [HttpGet("{vesselId:long}/media")]
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

    [HttpGet("{vesselId:long}/status-history")]
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

// ── Request bodies for the vessel upload/replace endpoints (B3/B4) ────────────────────────────

public sealed class VesselFileUploadUrlRequest
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSizeBytes { get; set; }
}

public sealed class RegisterVesselDocumentRequest
{
    public string FileId { get; set; } = default!;              // FileStorage FileId (Guid as string)
    public string UploadSessionCode { get; set; } = default!;
    public string DocumentTypeCode { get; set; } = default!;
    public string DocumentName { get; set; } = default!;
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class RegisterVesselMediaRequest
{
    public string FileId { get; set; } = default!;
    public string UploadSessionCode { get; set; } = default!;
    public VesselMediaType MediaType { get; set; } = VesselMediaType.Photo;
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }
}
