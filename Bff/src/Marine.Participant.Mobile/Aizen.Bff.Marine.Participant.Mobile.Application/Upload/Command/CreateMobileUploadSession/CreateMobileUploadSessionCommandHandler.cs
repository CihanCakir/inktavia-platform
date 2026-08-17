using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Upload;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Upload;

/// <summary>
/// Issue a client-side presigned upload session (M4f canonical pattern). ServerSideUpload=FALSE → FileStorage signs
/// the presigned PUT against the PUBLIC (device-reachable) endpoint, so the RN client PUTs the bytes straight to
/// storage. The BFF only brokers the session — no bytes flow through it.
/// </summary>
public sealed class CreateMobileUploadSessionCommandHandler
    : AizenCommandHandler<CreateMobileUploadSessionCommand, MobileUploadSessionResponse>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IFileStorageRemoteCall _fileStorage;

    public CreateMobileUploadSessionCommandHandler(
        IParticipantProfileResolver resolver, IFileStorageRemoteCall fileStorage)
    {
        _resolver = resolver;
        _fileStorage = fileStorage;
    }

    public override async Task<MobileUploadSessionResponse?> Handle(
        CreateMobileUploadSessionCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request ?? throw new AizenBusinessException("Upload request is required.");
        if (string.IsNullOrWhiteSpace(request.ContentType))
            throw new AizenBusinessException("A content type is required.");

        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var contentType = request.ContentType.Trim();
        var category = ResolveCategory(request.Category, contentType);

        var sessionResp = await _fileStorage.CreateUploadSession(new CreateUploadSessionRequest
        {
            OriginalFileName = string.IsNullOrWhiteSpace(request.FileName) ? "upload" : request.FileName,
            ContentType = contentType,
            SizeInBytes = request.SizeInBytes,
            Category = category,
            Visibility = FileVisibility.Private,
            OwnerModule = "Vessel",
            OwnerEntityType = "VesselMedia",
            // Client-side presigned: the URL must be signed for the public (device-reachable) endpoint.
            ServerSideUpload = false,
        });
        var session = sessionResp?.Body ?? throw new AizenBusinessException("Could not start the upload.");

        return new MobileUploadSessionResponse
        {
            FileId = session.FileId,
            UploadUrl = session.UploadUrl,
            UploadSessionCode = session.UploadSessionCode,
            ExpiresAt = session.ExpiresAt,
            // The presigned PUT is signed against this exact content type — the client MUST send it verbatim.
            RequiredContentType = contentType,
        };
    }

    private static FileCategory ResolveCategory(string? explicitCategory, string contentType)
    {
        if (!string.IsNullOrWhiteSpace(explicitCategory)
            && Enum.TryParse<FileCategory>(explicitCategory, ignoreCase: true, out var parsed))
            return parsed;

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return FileCategory.Image;
        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)) return FileCategory.Video;
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)) return FileCategory.Document;
        return FileCategory.Other;
    }
}
