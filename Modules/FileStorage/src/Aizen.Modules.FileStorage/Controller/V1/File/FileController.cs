using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Application.Commands.DeleteFile;
using Aizen.Modules.FileStorage.Application.Commands.LinkFileToOwner;
using Aizen.Modules.FileStorage.Application.Commands.UpdateFileVisibility;
using Aizen.Modules.FileStorage.Application.Queries.GetFileById;
using Aizen.Modules.FileStorage.Application.Queries.GetFileMetadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.FileStorage.Controller.V1.File;

[ApiController]
[Route("api/v1/files")]
[Tags("File")]
[Authorize]
[DocumentationInfo("File controller", "API endpoints for managing file resources in the FileStorage module.")]
public sealed class FileController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public FileController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private long CurrentUserId =>
        long.Parse(ContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{fileId:long}")]
    [ProducesResponseType(typeof(FileDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileDto?>> GetById(
        [FromRoute] long fileId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<FileDto>(new GetFileByIdQuery(fileId), ct);
        return SetResponse(result);
    }

    [HttpGet("{fileId:long}/metadata")]
    [ProducesResponseType(typeof(FileMetadataDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileMetadataDto?>> GetMetadata(
        [FromRoute] long fileId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<FileMetadataDto>(new GetFileMetadataQuery(fileId), ct);
        return SetResponse(result);
    }

    [HttpDelete("{fileId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(
        [FromRoute] long fileId,
        [FromBody] DeleteFileRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(
            new DeleteFileCommand { FileId = fileId, Request = req, UserId = CurrentUserId }, ct);
        return Ok(result);
    }

    [HttpPut("{fileId:long}/visibility")]
    [ProducesResponseType(typeof(FileDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileDto?>> UpdateVisibility(
        [FromRoute] long fileId,
        [FromBody] FileVisibility visibility,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<FileDto>(
            new UpdateFileVisibilityCommand { FileId = fileId, Visibility = visibility, UserId = CurrentUserId }, ct);
        return SetResponse(result);
    }

    [HttpPost("{fileId:long}/owners")]
    [ProducesResponseType(typeof(FileOwnerReferenceDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileOwnerReferenceDto?>> LinkToOwner(
        [FromRoute] long fileId,
        [FromBody] LinkFileToOwnerRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<FileOwnerReferenceDto>(
            new LinkFileToOwnerCommand { FileId = fileId, Request = req, UserId = CurrentUserId }, ct);
        return SetResponse(result);
    }
}
