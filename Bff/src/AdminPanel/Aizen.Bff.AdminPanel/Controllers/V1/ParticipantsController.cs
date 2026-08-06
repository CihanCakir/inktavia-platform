using Aizen.Bff.AdminPanel.Application.Identity.Query;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Participants")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class ParticipantsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ParticipantsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("identity/participant/profiles")]
    [ProducesResponseType(typeof(PagedParticipantProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PagedParticipantProfileResult>> SearchProfiles(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new SearchParticipantProfilesBffQuery(pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpGet("identity/participant/profiles/{profileId:guid}")]
    [ProducesResponseType(typeof(ParticipantProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ParticipantProfileResult>> GetProfileById(
        Guid profileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetParticipantProfileByIdBffQuery(profileId), ct);
        return SetResponse(result);
    }
}
