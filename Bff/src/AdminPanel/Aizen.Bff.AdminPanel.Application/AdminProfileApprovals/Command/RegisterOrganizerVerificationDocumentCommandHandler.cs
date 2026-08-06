using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Register organizer verification document BFF command handler", "Calls FileStorage to complete the upload session, then registers the document with Identity.")]
public sealed class RegisterOrganizerVerificationDocumentCommandHandler
    : AizenCommandHandler<RegisterOrganizerVerificationDocumentCommand, RegisterDocumentBffResponse>
{
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<RegisterOrganizerVerificationDocumentCommandHandler> _logger;

    public RegisterOrganizerVerificationDocumentCommandHandler(
        IFileStorageRemoteCall fileStorage,
        IIdentityRemoteCall identity,
        ILogger<RegisterOrganizerVerificationDocumentCommandHandler> logger)
    {
        _fileStorage = fileStorage;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<RegisterDocumentBffResponse?> Handle(
        RegisterOrganizerVerificationDocumentCommand request, CancellationToken cancellationToken)
    {
        var response = new RegisterDocumentBffResponse();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RegisterOrganizerDocument] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        // Step 1: Complete the upload session to confirm file physically exists in storage.
        var completeResult = await _fileStorage.CompleteDocumentUploadSession(
            request.UploadSessionCode,
            new CompleteDocumentUploadSessionRequest());

        if (completeResult?.Header?.IsSuccess != true)
        {
            _logger.LogWarning("[RegisterOrganizerDocument] FileStorage CompleteUploadSession failed: userId={UserId} profileId={ProfileId} errorCode={Code}",
                request.UserId, request.ProfileId, completeResult?.Header?.ErrorCode);
            response.Warnings.Add(AdminBffWarning.CallFailed("FileStorage", completeResult?.Header?.ErrorMessage ?? "Upload completion failed."));
            return response;
        }

        // Step 2: Register the document FileId with Identity.
        var identityResult = await _identity.RegisterOrganizerVerificationDocument(
            request.UserId,
            request.ProfileId,
            new RegisterVerificationDocumentRequest
            {
                FileId = request.FileId,
                DocumentType = request.DocumentType,
                Name = request.Name,
                Format = request.Format,
                FileSizeDisplay = request.FileSizeDisplay,
                Issuer = request.Issuer
            });

        if (identityResult?.Header?.IsSuccess != true || identityResult.Body == null)
        {
            _logger.LogWarning("[RegisterOrganizerDocument] Identity document registration failed: userId={UserId} profileId={ProfileId} errorCode={Code}",
                request.UserId, request.ProfileId, identityResult?.Header?.ErrorCode);
            response.Warnings.Add(AdminBffWarning.CallFailed("Identity", identityResult?.Header?.ErrorMessage ?? "Document registration failed."));
            return response;
        }

        _logger.LogInformation("[RegisterOrganizerDocument] Document registered: userId={UserId} profileId={ProfileId} documentId={DocId}",
            request.UserId, request.ProfileId, identityResult.Body.DocumentId);

        response.DocumentId = identityResult.Body.DocumentId;
        response.FileId = identityResult.Body.FileId;

        return response;
    }
}
