using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Request vessel file upload URL BFF command handler", "Creates a FileStorage upload session (OwnerModule=Vessel) and returns a pre-signed PUT URL for browser-direct upload.")]
public sealed class RequestVesselFileUploadUrlBffCommandHandler
    : AizenCommandHandler<RequestVesselFileUploadUrlBffCommand, VesselFileUploadUrlBffResponse>
{
    private readonly IFileStorageRemoteCall _fileStorage;

    public RequestVesselFileUploadUrlBffCommandHandler(IFileStorageRemoteCall fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public override async Task<VesselFileUploadUrlBffResponse?> Handle(
        RequestVesselFileUploadUrlBffCommand request, CancellationToken cancellationToken)
    {
        var response = new VesselFileUploadUrlBffResponse();

        var result = await _fileStorage.CreateDocumentUploadSession(new CreateDocumentUploadSessionRequest
        {
            OriginalFileName = request.FileName,
            ContentType = request.ContentType,
            SizeInBytes = request.FileSizeBytes,
            OwnerModule = "Vessel"
        });

        if (result?.Header?.IsSuccess != true || result.Body == null)
        {
            response.Warnings.Add(AdminBffWarning.CallFailed("FileStorage", result?.Header?.ErrorMessage ?? "Failed to create upload session."));
            return response;
        }

        response.FileId = result.Body.FileId.ToString();
        response.UploadUrl = result.Body.UploadUrl;
        response.UploadSessionCode = result.Body.UploadSessionCode;
        response.ExpiresAt = result.Body.ExpiresAt;
        return response;
    }
}
