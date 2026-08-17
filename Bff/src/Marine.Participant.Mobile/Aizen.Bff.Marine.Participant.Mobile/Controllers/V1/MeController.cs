using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Me;
using Aizen.Bff.Marine.Participant.Mobile.Application.Me;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

[ApiController]
[Route("api/v1/mobile/me")]
[Tags("Mobile - Me")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class MeController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MeController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Identity echo from the verified Keycloak token.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MeResponse>> GetMe(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMeQuery(), ct);
        return SetResponse(result);
    }
}
