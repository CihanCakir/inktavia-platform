using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.Processing;
using Aizen.Modules.FileStorage.Application.Commands.StartFileProcessing;
using Aizen.Modules.FileStorage.Application.Commands.UpdateFileProcessingResult;
using Aizen.Modules.FileStorage.Application.Queries.GetFileProcessingJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.FileStorage.Controller.V1.File;

[ApiController]
[Route("api/v1/files/{fileId:long}/processing")]
[Tags("FileProcessing")]
[Authorize]
[DocumentationInfo("File processing controller", "API endpoints for managing background processing jobs for files.")]
public sealed class FileProcessingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public FileProcessingController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(FileProcessingJobDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileProcessingJobDto?>> Start(
        [FromRoute] long fileId,
        [FromBody] StartFileProcessingRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<FileProcessingJobDto>(
            new StartFileProcessingCommand { FileId = fileId, Request = req }, ct);
        return SetResponse(result);
    }

    [HttpPut("result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateResult(
        [FromRoute] long fileId,
        [FromBody] UpdateFileProcessingResultRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(
            new UpdateFileProcessingResultCommand { FileId = fileId, Request = req }, ct);
        return Ok(result);
    }

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(IReadOnlyList<FileProcessingJobDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<FileProcessingJobDto>?>> GetJobs(
        [FromRoute] long fileId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<FileProcessingJobDto>>(
            new GetFileProcessingJobsQuery(fileId), ct);
        return SetResponse(result);
    }
}
