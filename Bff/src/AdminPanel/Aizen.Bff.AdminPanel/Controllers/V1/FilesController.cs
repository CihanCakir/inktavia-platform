using Aizen.Bff.AdminPanel.Application.Files.Command;
using Aizen.Bff.AdminPanel.Application.Files.Dto;
using Aizen.Bff.AdminPanel.Application.Files.Query;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/files")]
[Tags("Admin Panel - Files")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class FilesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public FilesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    // Admin dosya listesi. Önceden InactiveModulesController 501 dönüyordu; artık FileStorage'a proxy'lenir.
    [HttpGet]
    [ProducesResponseType(typeof(AdminFileListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminFileListResult>> GetFiles(
        [FromQuery] string? search,
        [FromQuery] string? contentType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminFileListBffQuery(search, contentType, page, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("{fileId:guid}")]
    [ProducesResponseType(typeof(AdminFileReviewOverviewResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminFileReviewOverviewResponse>> GetFileReviewOverview(
        Guid fileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetFileReviewOverviewBffQuery(fileId), ct);
        return SetResponse(result);
    }

    [HttpPost("bulk-read-urls")]
    [ProducesResponseType(typeof(List<FileAccessUrlResult>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<FileAccessUrlResult>>> BulkGenerateReadUrls(
        [FromBody] BulkGenerateReadUrlsRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new BulkGenerateReadUrlsBffCommand(request.FileIds, request.ExpiresInMinutes), ct);
        return SetResponse(result);
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> DeleteFile(
        Guid fileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new DeleteFileBffCommand(fileId), ct);
        return SetResponse(result);
    }

    [HttpPost("{fileId:guid}/read-url")]
    [ProducesResponseType(typeof(FileAccessUrlResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileAccessUrlResult>> CreateFileReadUrl(
        Guid fileId, [FromBody] CreateReadUrlRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CreateFileReadUrlBffCommand(fileId, request.ExpiresInMinutes), ct);
        return SetResponse(result);
    }

    [HttpPatch("{fileId:guid}/visibility")]
    [ProducesResponseType(typeof(Aizen.Bff.AdminPanel.Application.Common.RemoteClients.EmptyResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<Aizen.Bff.AdminPanel.Application.Common.RemoteClients.EmptyResult>> UpdateVisibility(
        Guid fileId, [FromBody] UpdateVisibilityRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateFileVisibilityBffCommand(fileId, request.Visibility), ct);
        return SetResponse(result);
    }
}

[DocumentationInfo("Bulk generate read URLs request", "Request body for generating pre-signed read URLs for multiple files.")]
public sealed class BulkGenerateReadUrlsRequest
{
    public List<Guid> FileIds { get; set; } = new();
    public int ExpiresInMinutes { get; set; } = 60;
}

public sealed class CreateReadUrlRequest { public int ExpiresInMinutes { get; set; } = 60; }
public sealed class UpdateVisibilityRequest { public string Visibility { get; set; } = string.Empty; }
