using System.Net.Http.Headers;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Aizen.Modules.Identity.Abstraction.Request;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Profile;

/// <summary>
/// Server-side avatar upload: resolve the participant (sets the identity holder), create a ServerSideUpload
/// FileStorage session (presigned PUT signed for the INTERNAL S3 endpoint so the BFF can push the bytes),
/// PUT the image, complete the session, then set the participant ProfilePhotoUrl to the fileId via the
/// asserted Identity update. The returned profile's avatarUrl is a fresh presigned read URL. Mirrors the
/// M3a update flow; the file ops use the service token's file_storage_write role.
/// </summary>
public sealed class UploadParticipantAvatarCommandHandler
    : AizenCommandHandler<UploadParticipantAvatarCommand, GetParticipantProfileResponse>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IIdentityRemoteCall _identity;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<UploadParticipantAvatarCommandHandler> _logger;

    public UploadParticipantAvatarCommandHandler(
        IParticipantProfileResolver resolver,
        IFileStorageRemoteCall fileStorage,
        IIdentityRemoteCall identity,
        IHttpClientFactory httpFactory,
        ILogger<UploadParticipantAvatarCommandHandler> logger)
    {
        _resolver = resolver;
        _fileStorage = fileStorage;
        _identity = identity;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public override async Task<GetParticipantProfileResponse?> Handle(
        UploadParticipantAvatarCommand request, CancellationToken cancellationToken)
    {
        if (request.Content is null || request.Content.Length == 0)
            throw new AizenBusinessException("No image was provided.");

        // Resolve the participant → sets the identity holder so the later Identity update is asserted.
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // 1) ServerSideUpload session — presigned PUT is signed for the internal S3 endpoint (minio:9000).
        var sessionResp = await _fileStorage.CreateUploadSession(new CreateUploadSessionRequest
        {
            OriginalFileName = string.IsNullOrWhiteSpace(request.FileName) ? "avatar" : request.FileName,
            ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType,
            SizeInBytes = request.SizeInBytes > 0 ? request.SizeInBytes : request.Content.LongLength,
            Category = FileCategory.Image,
            Visibility = FileVisibility.Private,
            OwnerModule = "Identity",
            OwnerEntityType = "ParticipantProfile",
            ServerSideUpload = true,
        });
        var session = sessionResp?.Body ?? throw new AizenBusinessException("Could not start the avatar upload.");

        // 2) Push the bytes to the presigned URL (plain client — the URL carries its own S3 signature; no Bearer).
        await UploadBytesAsync(session.UploadUrl, request.Content, request.ContentType, cancellationToken);

        // 3) Finalize the upload.
        var completed = await _fileStorage.CompleteUploadSession(session.UploadSessionCode, new CompleteUploadSessionRequest());
        var fileId = completed?.Body?.FileId ?? session.FileId;

        // 4) Point the participant profile at the stored file (asserted Identity update; profile table only).
        await _identity.UpdateParticipantProfile(new UpdateParticipantProfileRequest { ProfilePhotoUrl = fileId.ToString() });

        // 5) Echo the persisted profile with a fresh presigned avatar read URL.
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

    private async Task UploadBytesAsync(string url, byte[] content, string? contentType, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient(); // no delegating handler → no Authorization/assertion on the S3 PUT
        using var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        using var resp = await client.PutAsync(url, body, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Avatar S3 PUT failed {Status}: {Body}", (int)resp.StatusCode, text);
            throw new AizenBusinessException("Avatar upload to storage failed. Please try again.");
        }
    }
}
