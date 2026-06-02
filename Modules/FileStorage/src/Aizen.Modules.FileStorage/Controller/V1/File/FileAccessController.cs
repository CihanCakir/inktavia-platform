using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Application.Commands.CreateReadUrl;
using Aizen.Modules.FileStorage.Application.Queries.ValidateFileOwnership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.FileStorage.Controller.V1.File;

[ApiController]
[Route("api/v1/files/{fileId:long}/access")]
[Tags("FileAccess")]
[Authorize]
[DocumentationInfo("File access controller", "API endpoints for generating pre-signed URLs and validating file ownership.")]
public sealed class FileAccessController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public FileAccessController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private long CurrentUserId =>
        long.Parse(ContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("read-url")]
    [ProducesResponseType(typeof(FileAccessUrlDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileAccessUrlDto?>> CreateReadUrl(
        [FromRoute] long fileId,
        [FromBody] CreateReadUrlRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<FileAccessUrlDto>(
            new CreateReadUrlCommand { FileId = fileId, Request = req, UserId = CurrentUserId }, ct);
        return SetResponse(result);
    }

    [HttpPost("validate-ownership")]
    [ProducesResponseType(typeof(FileValidationResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileValidationResultDto?>> ValidateOwnership(
        [FromRoute] long fileId,
        [FromBody] ValidateFileOwnershipRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<FileValidationResultDto>(
            new ValidateFileOwnershipQuery(fileId, req), ct);
        return SetResponse(result);
    }
}
