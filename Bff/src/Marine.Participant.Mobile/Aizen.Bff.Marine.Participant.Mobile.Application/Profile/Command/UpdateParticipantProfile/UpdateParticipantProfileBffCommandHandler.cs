using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Request;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Profile;

/// <summary>
/// Update flow: (1) resolve the participant by Keycloak subject — this populates the identity holder so the
/// subsequent call is asserted as this participant; (2) call the Identity participant-profile update (asserted,
/// so Identity resolves UserInfo.UserId to the right row); (3) re-resolve and return the fresh profile.
/// The Identity update writes the participant profile table only (NOT Keycloak), so no Keycloak declarative-profile
/// GET→PUT is needed and the change is immediately visible via /profile/me (the JWT `name` claim is unaffected).
/// </summary>
public sealed class UpdateParticipantProfileBffCommandHandler
    : AizenCommandHandler<UpdateParticipantProfileBffCommand, GetParticipantProfileResponse>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IIdentityRemoteCall _identity;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<UpdateParticipantProfileBffCommandHandler> _logger;

    public UpdateParticipantProfileBffCommandHandler(
        IParticipantProfileResolver resolver,
        IIdentityRemoteCall identity,
        IFileStorageRemoteCall fileStorage,
        ILogger<UpdateParticipantProfileBffCommandHandler> logger)
    {
        _resolver = resolver;
        _identity = identity;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<GetParticipantProfileResponse?> Handle(
        UpdateParticipantProfileBffCommand request, CancellationToken cancellationToken)
    {
        // 1) Resolve → sets the identity holder (asserts identity on the update call below).
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // 2) Asserted update against the existing Identity participant endpoint.
        await _identity.UpdateParticipantProfile(new UpdateParticipantProfileRequest
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Bio = request.Bio,
            Gender = request.Gender,
            BirthDate = request.BirthDate,
            NationalityId = request.NationalityId,
        });

        // 3) Re-resolve to echo the persisted profile back to the client (with a fresh avatar read-url).
        var refreshed = await _resolver.ResolveAsync(cancellationToken);
        var profile = refreshed.Profile is null ? null : GetParticipantProfileQueryHandler.MapProfile(refreshed.Profile);
        await GetParticipantProfileQueryHandler.ResolveAvatarUrlAsync(profile, _fileStorage, _logger, cancellationToken);
        return new GetParticipantProfileResponse
        {
            HasProfileLink = refreshed.ProfileId is > 0,
            Profile = profile,
            Message = "OK",
        };
    }
}
