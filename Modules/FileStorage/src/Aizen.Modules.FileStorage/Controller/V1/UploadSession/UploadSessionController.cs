using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Aizen.Modules.FileStorage.Application.Commands.CompleteUploadSession;
using Aizen.Modules.FileStorage.Application.Commands.CreateUploadSession;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.FileStorage.Controller.V1.UploadSession;

[ApiController]
[Route("api/v1/upload-sessions")]
[Tags("UploadSession")]
[Authorize]
[DocumentationInfo("Upload session controller", "API endpoints for managing S3 pre-signed upload sessions.")]
public sealed class UploadSessionController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public UploadSessionController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FileUploadSessionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<FileUploadSessionDto?>> Create(
        [FromBody] CreateUploadSessionRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<FileUploadSessionDto>(
            new CreateUploadSessionCommand { Request = req }, ct);
        return SetResponse(result);
    }

    [HttpPost("{uploadSessionCode}/complete")]
    [ProducesResponseType(typeof(Abstraction.Dto.File.FileDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<Abstraction.Dto.File.FileDto?>> Complete(
        [FromRoute] string uploadSessionCode,
        [FromBody] CompleteUploadSessionRequest req,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<Abstraction.Dto.File.FileDto>(
            new CompleteUploadSessionCommand
            {
                UploadSessionCode = uploadSessionCode,
                Request = req
            }, ct);
        return SetResponse(result);
    }
}
