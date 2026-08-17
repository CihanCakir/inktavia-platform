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
/// BE_MO10a — the owner chat thread for one service request: read the thread (Messaging store, participant-authorized)
/// and send a TEXT message (SR module, SenderType=Owner; the anti-harassment gate is provider-only). Identity from the
/// token; the owner can only read/write their own SR threads. Image/location are MO10b/MO10c. Cost-free.
/// </summary>
[ApiController]
[Route("api/v1/mobile/service-requests/{serviceRequestId:long}/messages")]
[Tags("Mobile - Chat")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class ChatController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ChatController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The caller's chat thread for one of their own SRs (403 module-side if not a participant).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MobileChatThreadDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileChatThreadDto>> GetThread(
        [FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileChatThreadQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    /// <summary>Send a message on one of the caller's own SRs — exactly one of { text, image, location }
    /// (SenderType=Owner; identity from the token). Image = an AttachmentFileId from the reused upload session.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MobileChatMessageDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileChatMessageDto>> Send(
        [FromRoute] long serviceRequestId, [FromBody] MobileSendChatMessageRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new SendMobileChatMessageCommand(request ?? new MobileSendChatMessageRequest(), serviceRequestId), ct);
        return SetResponse(result);
    }
}
