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
/// BE_MO10a — the owner chat inbox: the owner's service-request conversations from the unified Messaging store,
/// scoped to the caller by the assertion (no user id on the wire). Cost-free.
/// </summary>
[ApiController]
[Route("api/v1/mobile/conversations")]
[Tags("Mobile - Chat")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class MobileConversationsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MobileConversationsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The caller's chat inbox (paged; SR conversations with last-message preview + unread count).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MobileConversationListDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileConversationListDto>> GetInbox(
        [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileConversationsQuery(skip, take), ct);
        return SetResponse(result);
    }
}
