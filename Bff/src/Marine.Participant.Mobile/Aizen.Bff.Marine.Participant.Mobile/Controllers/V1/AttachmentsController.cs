using Aizen.Bff.Marine.Participant.Mobile.Application.Chat;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>
/// BE_MO10b — owner-scoped signed read-urls for chat/attachment files on the owner's own service requests. The SR
/// module owner-gates (owns the SR + the fileId is on the SR); the BFF mints the short-TTL presigned GET. Identity
/// from the token; an owner can only read files on their own SR threads. Cost-free.
/// </summary>
[ApiController]
[Route("api/v1/mobile/service-requests/{serviceRequestId:long}/attachments")]
[Tags("Mobile - Chat")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class AttachmentsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AttachmentsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>A short-lived signed read-url for a file the owner is authorized to view (empty url when the object
    /// can't be resolved — the FE shows a placeholder).</summary>
    [HttpGet("{fileId:guid}/read-url")]
    [ProducesResponseType(typeof(MobileAttachmentReadUrlDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileAttachmentReadUrlDto>> GetReadUrl(
        [FromRoute] long serviceRequestId, [FromRoute] Guid fileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileAttachmentReadUrlQuery(serviceRequestId, fileId), ct);
        return SetResponse(result);
    }
}
