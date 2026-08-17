using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Bff.Marine.Participant.Mobile.Application.Profile;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

[ApiController]
[Route("api/v1/mobile/profile")]
[Tags("Mobile - Profile")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class ProfileController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProfileController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The authenticated participant's profile (name, email, phone, avatar, …).</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(GetParticipantProfileResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetParticipantProfileResponse>> GetMe(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetParticipantProfileQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>Update the participant's editable profile fields; returns the persisted profile.</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(GetParticipantProfileResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetParticipantProfileResponse>> UpdateMe(
        [FromBody] UpdateParticipantProfileBffRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new UpdateParticipantProfileBffCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Bio = request.Bio,
            Gender = request.Gender,
            BirthDate = request.BirthDate,
            NationalityId = request.NationalityId,
        }, ct);
        return SetResponse(result);
    }

    /// <summary>Set the participant's avatar to a completed client-side upload (`{ fileId }`); returns the updated
    /// profile with a fresh avatarUrl. The image bytes are uploaded directly to storage via /mobile/uploads —
    /// never through this BFF.</summary>
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(GetParticipantProfileResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetParticipantProfileResponse>> UploadAvatar(
        [FromBody] UploadParticipantAvatarRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new UploadParticipantAvatarCommand { FileId = request.FileId }, ct);
        return SetResponse(result);
    }
}
