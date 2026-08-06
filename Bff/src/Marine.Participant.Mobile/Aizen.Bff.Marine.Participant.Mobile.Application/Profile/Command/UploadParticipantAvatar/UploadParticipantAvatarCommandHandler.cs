using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Request;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Profile;

/// <summary>
/// Attach a completed client-side upload as the participant avatar (M3c, retrofitted to the M4f client-side
/// presigned flow). The image bytes were PUT directly to storage via /mobile/uploads — the BFF only points the
/// profile's ProfilePhotoUrl at the fileId (asserted Identity update) and echoes the profile with a fresh presigned
/// avatar read URL. No bytes flow through the BFF.
/// </summary>
public sealed class UploadParticipantAvatarCommandHandler
    : AizenCommandHandler<UploadParticipantAvatarCommand, GetParticipantProfileResponse>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<UploadParticipantAvatarCommandHandler> _logger;

    public UploadParticipantAvatarCommandHandler(
        IParticipantProfileResolver resolver,
        IFileStorageRemoteCall fileStorage,
        IIdentityRemoteCall identity,
        ILogger<UploadParticipantAvatarCommandHandler> logger)
    {
        _resolver = resolver;
        _fileStorage = fileStorage;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<GetParticipantProfileResponse?> Handle(
        UploadParticipantAvatarCommand request, CancellationToken cancellationToken)
    {
        if (request.FileId == Guid.Empty)
            throw new AizenBusinessException("A fileId is required.");

        // Resolve the participant → sets the identity holder so the Identity update is asserted.
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // Point the participant profile at the stored file (asserted Identity update; profile table only).
        await _identity.UpdateParticipantProfile(new UpdateParticipantProfileRequest
        {
            ProfilePhotoUrl = request.FileId.ToString(),
        });

        // Echo the persisted profile with a fresh presigned avatar read URL.
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
