using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Bff.MarineProvider.Application.Files;
using Aizen.Bff.MarineProvider.Application.Files;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1.Files;

[ApiController]
[Route("api/v1/provider/files")]
[Tags("Provider - Files")]
// PROV-MVP-045 — a bare [Authorize] resolves to the framework default (authenticated only), so ANY subject in
// the realm — including one with no provider profile at all — could mint upload sessions; the only limit was a
// per-provider rate limit. Onboarding legitimately needs a not-yet-active provider to upload, so this is
// PendingOrActive rather than ProviderActive: a linked, non-suspended profile.
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderPendingOrActive)]
public sealed class FileController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public FileController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) { _cqrs = cqrs; }

    [HttpPost("upload-session")]
    [ProducesResponseType(typeof(CreateUploadSessionBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateUploadSessionBffResponse>> CreateUploadSession(
        [FromBody] CreateUploadSessionBffRequest request, CancellationToken ct)
    {
        var command = new CreateUploadSessionCommand
        {
            FileName = request.FileName,
            ContentType = request.ContentType,
            Size = request.Size,
            Category = request.Category,
        };
        return SetResponse(await _cqrs.ProcessAsync(command, ct));
    }

    [HttpPost("{fileId:guid}/complete")]
    [ProducesResponseType(typeof(CompleteUploadBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CompleteUploadBffResponse>> CompleteUpload(
        Guid fileId, [FromBody] CompleteUploadBffRequest request, CancellationToken ct)
    {
        var command = new CompleteUploadCommand
        {
            FileId = fileId,
            UploadSessionCode = request.UploadSessionCode,
        };
        return SetResponse(await _cqrs.ProcessAsync(command, ct));
    }
}
