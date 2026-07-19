using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding;

public sealed class GetDocumentAccessUrlCommandHandler
    : AizenCommandHandler<GetDocumentAccessUrlCommand, DocumentAccessUrlBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IIdentityRemoteCall _identity;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetDocumentAccessUrlCommandHandler> _logger;

    public GetDocumentAccessUrlCommandHandler(
        IProviderProfileResolver resolver,
        IIdentityRemoteCall identity,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetDocumentAccessUrlCommandHandler> logger)
    {
        _resolver = resolver;
        _identity = identity;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<DocumentAccessUrlBffResponse?> Handle(GetDocumentAccessUrlCommand request, CancellationToken ct)
    {
        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module calls.
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0) return null;

        try
        {
            // A read URL is a capability. Minting one for any FileId the caller names is an IDOR: the caller
            // could read another provider's documents. Only files that are attached to THIS profile may be read.
            var onboarding = await _identity.GetProviderOnboarding(profileId);
            var owns = onboarding?.Body?.Documents?.Any(d => d.FileId == request.FileId) ?? false;
            if (!owns)
            {
                _logger.LogWarning("Read-url denied: file {FileId} is not attached to profile {ProfileId}.",
                    request.FileId, profileId);
                return null;
            }

            var result = await _fileStorage.CreateReadUrl(request.FileId, new CreateReadUrlRequest
            {
                ExpiresIn = TimeSpan.FromMinutes(5),
            });

            var data = result.Body;
            if (data is null) return null;

            return new DocumentAccessUrlBffResponse
            {
                Url = data.ReadUrl,
                ExpiresAt = data.ExpiresAt,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access URL for file {FileId}.", request.FileId);
            return null;
        }
    }
}
