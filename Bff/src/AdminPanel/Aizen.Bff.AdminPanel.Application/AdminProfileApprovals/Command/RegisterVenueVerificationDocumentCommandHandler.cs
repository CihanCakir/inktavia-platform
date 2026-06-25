using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Register venue verification document BFF command handler", "Calls FileStorage to complete the upload session, then registers the document with Identity for a venue profile.")]
public sealed class RegisterVenueVerificationDocumentCommandHandler
    : AizenCommandHandler<RegisterVenueVerificationDocumentCommand, RegisterDocumentBffResponse>
{
    private readonly IFileStorageAdminBffRemoteCall _fileStorage;
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    private readonly ILogger<RegisterVenueVerificationDocumentCommandHandler> _logger;

    public RegisterVenueVerificationDocumentCommandHandler(
        IFileStorageAdminBffRemoteCall fileStorage,
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        ILogger<RegisterVenueVerificationDocumentCommandHandler> logger)
    {
        _fileStorage = fileStorage;
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
        _logger = logger;
    }

    public override async Task<RegisterDocumentBffResponse?> Handle(
        RegisterVenueVerificationDocumentCommand request, CancellationToken cancellationToken)
    {
        var response = new RegisterDocumentBffResponse();

        string authHeader;
        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            authHeader = $"Bearer {serviceToken}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RegisterVenueDocument] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        // Step 1: Complete the upload session to confirm file physically exists in storage.
        var completeResult = await _fileStorage.CompleteDocumentUploadSession(
            request.UploadSessionCode,
            new CompleteDocumentUploadSessionRequest(),
            authHeader,
            request.UserToken);

        if (completeResult?.Header?.IsSuccess != true)
        {
            _logger.LogWarning("[RegisterVenueDocument] FileStorage CompleteUploadSession failed: userId={UserId} profileId={ProfileId} errorCode={Code}",
                request.UserId, request.ProfileId, completeResult?.Header?.ErrorCode);
            response.Warnings.Add(AdminBffWarning.CallFailed("FileStorage", completeResult?.Header?.ErrorMessage ?? "Upload completion failed."));
            return response;
        }

        // Step 2: Register the document FileId with Identity.
        var identityResult = await _identity.RegisterVenueVerificationDocument(
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
            },
            authHeader,
            request.UserToken);

        if (identityResult?.Header?.IsSuccess != true || identityResult.Body == null)
        {
            _logger.LogWarning("[RegisterVenueDocument] Identity document registration failed: userId={UserId} profileId={ProfileId} errorCode={Code}",
                request.UserId, request.ProfileId, identityResult?.Header?.ErrorCode);
            response.Warnings.Add(AdminBffWarning.CallFailed("Identity", identityResult?.Header?.ErrorMessage ?? "Document registration failed."));
            return response;
        }

        _logger.LogInformation("[RegisterVenueDocument] Document registered: userId={UserId} profileId={ProfileId} documentId={DocId}",
            request.UserId, request.ProfileId, identityResult.Body.DocumentId);

        response.DocumentId = identityResult.Body.DocumentId;
        response.FileId = identityResult.Body.FileId;

        return response;
    }
}
