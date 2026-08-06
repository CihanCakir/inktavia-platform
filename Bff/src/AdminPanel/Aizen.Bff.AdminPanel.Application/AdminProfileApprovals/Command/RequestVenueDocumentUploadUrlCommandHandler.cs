using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Request venue document upload URL BFF command handler", "Calls FileStorage to create an upload session and returns a pre-signed PUT URL for browser-direct upload.")]
public sealed class RequestVenueDocumentUploadUrlCommandHandler
    : AizenCommandHandler<RequestVenueDocumentUploadUrlCommand, DocumentUploadUrlBffResponse>
{
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<RequestVenueDocumentUploadUrlCommandHandler> _logger;

    public RequestVenueDocumentUploadUrlCommandHandler(
        IFileStorageRemoteCall fileStorage,
        ILogger<RequestVenueDocumentUploadUrlCommandHandler> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<DocumentUploadUrlBffResponse?> Handle(
        RequestVenueDocumentUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var response = new DocumentUploadUrlBffResponse();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RequestVenueUploadUrl] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        var uploadRequest = new CreateDocumentUploadSessionRequest
        {
            OriginalFileName = request.FileName,
            ContentType = request.ContentType,
            SizeInBytes = request.FileSizeBytes,
            OwnerModule = "Identity"
        };

        var result = await _fileStorage.CreateDocumentUploadSession(uploadRequest);

        if (result?.Header?.IsSuccess != true || result.Body == null)
        {
            _logger.LogWarning("[RequestVenueUploadUrl] FileStorage upload session creation failed: userId={UserId} profileId={ProfileId} errorCode={Code}",
                request.UserId, request.ProfileId, result?.Header?.ErrorCode);
            response.Warnings.Add(AdminBffWarning.CallFailed("FileStorage", result?.Header?.ErrorMessage ?? "Failed to create upload session."));
            return response;
        }

        _logger.LogInformation("[RequestVenueUploadUrl] Upload session created: userId={UserId} profileId={ProfileId} fileId={FileId}",
            request.UserId, request.ProfileId, result.Body.FileId);

        response.FileId = result.Body.FileId.ToString();
        response.UploadUrl = result.Body.UploadUrl;
        response.UploadSessionCode = result.Body.UploadSessionCode;
        response.ExpiresAt = result.Body.ExpiresAt;

        return response;
    }
}
