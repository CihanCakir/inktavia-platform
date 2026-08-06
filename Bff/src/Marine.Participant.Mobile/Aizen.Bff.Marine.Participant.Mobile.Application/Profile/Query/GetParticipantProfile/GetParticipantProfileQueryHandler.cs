using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Profile;

public sealed class GetParticipantProfileQueryHandler
    : AizenQueryHandler<GetParticipantProfileQuery, GetParticipantProfileResponse>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetParticipantProfileQueryHandler> _logger;

    public GetParticipantProfileQueryHandler(
        IParticipantProfileResolver resolver,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetParticipantProfileQueryHandler> logger)
    {
        _resolver = resolver;
        _fileStorage = fileStorage;
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
            await ResolveAvatarUrlAsync(response.Profile, _fileStorage, _logger, cancellationToken);
            response.Message = "OK";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Participant profile resolution failed.");
            response.Message = "Profile is temporarily unavailable.";
        }

        return response;
    }

    /// <summary>
    /// When ProfilePhotoUrl holds a FileStorage fileId (M3c avatar), swap AvatarUrl for a fresh presigned read URL
    /// the client can render. Legacy non-Guid values pass through untouched; a resolution failure nulls the avatar.
    /// </summary>
    internal static async Task ResolveAvatarUrlAsync(
        ParticipantProfileDto? profile, IFileStorageRemoteCall fileStorage, ILogger logger, CancellationToken ct)
    {
        if (profile?.AvatarUrl is null || !Guid.TryParse(profile.AvatarUrl, out var fileId))
            return;

        try
        {
            var res = await fileStorage.CreateReadUrl(fileId, new CreateReadUrlRequest { ExpiresIn = TimeSpan.FromHours(6) });
            var url = res?.Body?.ReadUrl;
            profile.AvatarUrl = string.IsNullOrEmpty(url) ? null : url;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Avatar read-url resolution failed for file {FileId}.", fileId);
            profile.AvatarUrl = null;
        }
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
