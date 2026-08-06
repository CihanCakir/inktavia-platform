using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Profile;

public sealed class GetParticipantProfileQueryHandler
    : AizenQueryHandler<GetParticipantProfileQuery, GetParticipantProfileResponse>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly ILogger<GetParticipantProfileQueryHandler> _logger;

    public GetParticipantProfileQueryHandler(
        IParticipantProfileResolver resolver,
        ILogger<GetParticipantProfileQueryHandler> logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    public override async Task<GetParticipantProfileResponse?> Handle(
        GetParticipantProfileQuery request, CancellationToken cancellationToken)
    {
        var response = new GetParticipantProfileResponse();

        try
        {
            var resolution = await _resolver.ResolveAsync(cancellationToken);
            response.HasProfileLink = resolution.ProfileId is > 0;

            if (resolution.Profile is null)
            {
                response.Message = "No participant profile is linked to this account yet.";
                return response;
            }

            response.Profile = MapProfile(resolution.Profile);
            response.Message = "OK";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Participant profile resolution failed.");
            response.Message = "Profile is temporarily unavailable.";
        }

        return response;
    }

    /// <summary>Shared DTO → mobile contract mapping. FullName = First + Last; AvatarUrl = ProfilePhotoUrl.</summary>
    internal static ParticipantProfileDto MapProfile(OrganizerProfileDetailDto dto)
    {
        var fullName = string.Join(" ", new[] { dto.FirstName, dto.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

        return new ParticipantProfileDto
        {
            ParticipantProfileId = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
            Email = dto.Email,
            Phone = dto.Phone,
            AvatarUrl = dto.ProfilePhotoUrl,
            Bio = dto.Bio,
            Gender = dto.Gender,
            BirthDate = dto.BirthDate,
            NationalityId = dto.NationalityId,
            ApprovalStatus = dto.ApprovalStatus,
            ProfileStatus = dto.Status,
        };
    }
}
