using Aizen.Bff.AdminPanel.Application.AdminFiles.Command;
using Aizen.Bff.AdminPanel.Application.AdminFiles.Dto;
using Aizen.Bff.AdminPanel.Application.AdminFiles.Query;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Files")]
[Authorize]
public sealed class FilesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public FilesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("files")]
    [ProducesResponseType(typeof(List<AdminFileReviewOverviewResponse>), StatusCodes.Status200OK)]
    public Task<AizenApiResponse<List<AdminFileReviewOverviewResponse>>> ListFiles(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Stub: file listing is managed via FileStorage module's own admin API.
        // Returns empty list until a dedicated BFF listing query/remote-call is wired up.
        return Task.FromResult(SetResponse(new List<AdminFileReviewOverviewResponse>()));
    }

    [HttpGet("files/{fileId:long}")]
    [ProducesResponseType(typeof(AdminFileReviewOverviewResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminFileReviewOverviewResponse>> GetFileReviewOverview(
        long fileId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminFileReviewOverviewQuery(fileId, userToken), ct);
        return SetResponse(result);
    }

    [HttpPost("files/bulk-read-urls")]
    [ProducesResponseType(typeof(List<FileAccessUrlResult>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<FileAccessUrlResult>>> BulkGenerateReadUrls(
        [FromBody] BulkGenerateReadUrlsRequest request, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new BulkGenerateReadUrlsCommand(request.FileIds, request.ExpiresInMinutes, userToken), ct);
        return SetResponse(result);
    }

    [HttpDelete("files/{fileId:long}")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> DeleteFile(
        long fileId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new DeleteFileCommand(fileId, userToken), ct);
        return SetResponse(result);
    }

    [HttpPost("files/{fileId:long}/read-url")]
    [ProducesResponseType(typeof(FileAccessUrlResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileAccessUrlResult>> CreateFileReadUrl(
        long fileId, [FromBody] CreateReadUrlRequest request, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new CreateFileReadUrlCommand(fileId, request.ExpiresInMinutes, userToken), ct);
        return SetResponse(result);
    }

    [HttpPatch("files/{fileId:long}/visibility")]
    [ProducesResponseType(typeof(Aizen.Bff.AdminPanel.Application.Common.RemoteClients.EmptyResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<Aizen.Bff.AdminPanel.Application.Common.RemoteClients.EmptyResult>> UpdateVisibility(
        long fileId, [FromBody] UpdateVisibilityRequest request, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new UpdateFileVisibilityCommand(fileId, request.Visibility, userToken), ct);
        return SetResponse(result);
    }
}

[DocumentationInfo("Bulk generate read URLs request", "Request body for generating pre-signed read URLs for multiple files.")]
public sealed class BulkGenerateReadUrlsRequest
{
    public List<long> FileIds { get; set; } = new();
    public int ExpiresInMinutes { get; set; } = 60;
}

public sealed class CreateReadUrlRequest { public int ExpiresInMinutes { get; set; } = 60; }
public sealed class UpdateVisibilityRequest { public string Visibility { get; set; } = string.Empty; }
