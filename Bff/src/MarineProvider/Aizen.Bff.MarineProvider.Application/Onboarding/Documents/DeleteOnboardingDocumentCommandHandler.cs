using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding.Documents;

public sealed class DeleteOnboardingDocumentCommandHandler
    : AizenCommandHandler<DeleteOnboardingDocumentCommand, DeleteDocumentBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<DeleteOnboardingDocumentCommandHandler> _logger;

    public DeleteOnboardingDocumentCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IProviderIdentityRemoteCall identity,
        ILogger<DeleteOnboardingDocumentCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<DeleteDocumentBffResponse?> Handle(DeleteOnboardingDocumentCommand request, CancellationToken ct)
    {
        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module call.
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0)
            return new DeleteDocumentBffResponse { Success = false, Message = "Provider profile not found." };

        if (_identityHolder.UserId is not > 0)
        {
            _logger.LogError("Provider identity unresolved for profile {ProfileId}; refusing to delete document.", profileId);
            return new DeleteDocumentBffResponse { Success = false, Message = "Provider identity could not be resolved." };
        }

        try
        {
            // Remove the document row from Identity first
            var identityResult = await _identity.RemoveProviderDocument(profileId, request.FileId);
            var data = identityResult.Body;
            if (data is null || !data.Success)
                return new DeleteDocumentBffResponse { Success = false, Message = "Failed to remove document from profile." };

            // NOTE: FileStorage file deletion (soft-delete) can be triggered asynchronously by Identity
            // or handled here if needed. For now, Identity owns the document lifecycle.

            return new DeleteDocumentBffResponse
            {
                Success = true,
                Message = "Document removed successfully.",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {FileId} for profile {ProfileId}.", request.FileId, profileId);
            return new DeleteDocumentBffResponse { Success = false, Message = "An error occurred while deleting the document." };
        }
    }
}
