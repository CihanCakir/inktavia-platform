using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.Common.Abstraction.ViewModel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding;

public sealed class DeleteOnboardingDocumentCommandHandler
    : AizenCommandHandler<DeleteOnboardingDocumentCommand, DeleteDocumentBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<DeleteOnboardingDocumentCommandHandler> _logger;

    public DeleteOnboardingDocumentCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IIdentityRemoteCall identity,
        ILogger<DeleteOnboardingDocumentCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _identity = identity;
        _logger = logger;
    }

    // FAZ13A #67: başarısızlık 200'de gizlenmiyor — AizenBusinessException olarak fırlatılıyor (bkz. SaveOnboardingStep).
    public override async Task<DeleteDocumentBffResponse?> Handle(DeleteOnboardingDocumentCommand request, CancellationToken ct)
    {
        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module call.
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderProfileNotFound, "Provider profile not found.");

        // Fail closed: a missing user id is a bug, not a caller we can silently treat as user 0.
        if (_identityHolder.UserId is not > 0)
        {
            _logger.LogError("Provider identity unresolved for profile {ProfileId}; refusing to delete document.", profileId);
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingCallerIdentityInvalid, "Provider identity could not be resolved.");
        }

        RemoveProviderDocumentResponse? data;
        try
        {
            var identityResult = await _identity.RemoveProviderDocument(profileId, request.FileId);
            data = identityResult.Body;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Delete document {FileId} rejected for profile {ProfileId}.", request.FileId, profileId);
            throw ModuleFailurePropagation.FromApiException(ex, "An error occurred while deleting the document.");
        }

        if (data is null || !data.Success)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingDocumentNotFound, "Failed to remove document from profile.");

        return new DeleteDocumentBffResponse { Success = true, Message = "Document removed successfully." };
    }
}
