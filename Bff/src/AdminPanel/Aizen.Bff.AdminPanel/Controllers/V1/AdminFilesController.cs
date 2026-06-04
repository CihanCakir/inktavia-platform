using Aizen.Bff.AdminPanel.Application.AdminFiles.Command;
using Aizen.Bff.AdminPanel.Application.AdminFiles.Dto;
using Aizen.Bff.AdminPanel.Application.AdminFiles.Query;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Files")]
[Authorize(Roles = "Admin")]
public sealed class AdminFilesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminFilesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("files/{fileId:guid}")]
    [ProducesResponseType(typeof(AdminFileReviewOverviewResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminFileReviewOverviewResponse>> GetFileReviewOverview(
        Guid fileId, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetAdminFileReviewOverviewQuery(fileId, auth), ct);
        return SetResponse(result);
    }

    [HttpPost("files/bulk-read-urls")]
    [ProducesResponseType(typeof(List<FileAccessUrlResult>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<FileAccessUrlResult>>> BulkGenerateReadUrls(
        [FromBody] BulkGenerateReadUrlsRequest request, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new BulkGenerateReadUrlsCommand(request.FileIds, request.ExpiresInMinutes, auth), ct);
        return SetResponse(result);
    }

    [HttpDelete("files/{fileId:guid}")]
    [ProducesResponseType(typeof(AdminBffCommandResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminBffCommandResultDto>> DeleteFile(
        Guid fileId, CancellationToken ct)
    {
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new DeleteFileCommand(fileId, auth), ct);
        return SetResponse(result);
    }
}

[DocumentationInfo("Bulk generate read URLs request", "Request body for generating pre-signed read URLs for multiple files.")]
public sealed class BulkGenerateReadUrlsRequest
{
    public List<Guid> FileIds { get; set; } = new();
    public int ExpiresInMinutes { get; set; } = 60;
}
