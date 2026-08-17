using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Upload;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Upload;

/// <summary>Complete a client-side presigned upload (M4f). Delegates to FileStorage complete, which reads the object
/// back from storage (fails if the client never PUT it) and marks the file Ready; returns the committed fileId.</summary>
public sealed class CompleteMobileUploadCommandHandler
    : AizenCommandHandler<CompleteMobileUploadCommand, MobileUploadCompleteResponse>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IFileStorageRemoteCall _fileStorage;

    public CompleteMobileUploadCommandHandler(
        IParticipantProfileResolver resolver, IFileStorageRemoteCall fileStorage)
    {
        _resolver = resolver;
        _fileStorage = fileStorage;
    }

    public override async Task<MobileUploadCompleteResponse?> Handle(
        CompleteMobileUploadCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.UploadSessionCode))
            throw new AizenBusinessException("An upload session code is required.");

        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var completed = await _fileStorage.CompleteUploadSession(
            command.UploadSessionCode, new CompleteUploadSessionRequest());
        var fileId = completed?.Body?.FileId
            ?? throw new AizenBusinessException("Upload could not be finalized (the file was not found in storage).");

        return new MobileUploadCompleteResponse { FileId = fileId };
    }
}
