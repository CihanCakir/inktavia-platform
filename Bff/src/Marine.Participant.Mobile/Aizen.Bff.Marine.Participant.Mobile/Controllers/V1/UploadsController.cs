using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Upload;
using Aizen.Bff.Marine.Participant.Mobile.Application.Upload;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>Canonical client-side presigned upload primitive (M4f). `session` issues a presigned PUT URL the client
/// uploads to DIRECTLY (bytes never traverse the BFF); `complete` finalizes it → fileId. Reusable by media, docs
/// and avatar going forward.</summary>
[ApiController]
[Route("api/v1/mobile/uploads")]
[Tags("Mobile - Uploads")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class UploadsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public UploadsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Request a presigned PUT session. The client PUTs raw bytes to <c>uploadUrl</c> with the exact
    /// <c>requiredContentType</c> header, then calls <c>complete</c>.</summary>
    [HttpPost("session")]
    [ProducesResponseType(typeof(MobileUploadSessionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileUploadSessionResponse>> CreateSession(
        [FromBody] CreateMobileUploadSessionRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new CreateMobileUploadSessionCommand(request), ct);
        return SetResponse(result);
    }

    /// <summary>Finalize an upload after the client's PUT; returns the committed fileId to attach.</summary>
    [HttpPost("complete")]
    [ProducesResponseType(typeof(MobileUploadCompleteResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileUploadCompleteResponse>> Complete(
        [FromBody] CompleteMobileUploadRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new CompleteMobileUploadCommand(request.UploadSessionCode), ct);
        return SetResponse(result);
    }
}
