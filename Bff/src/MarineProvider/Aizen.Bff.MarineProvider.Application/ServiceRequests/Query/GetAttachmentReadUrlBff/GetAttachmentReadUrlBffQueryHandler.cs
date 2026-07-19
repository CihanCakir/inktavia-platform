using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// Mints a short-lived signed read URL for a service request attachment.
/// Access-scoped: the module verifies the provider may see the request AND the fileId is an attachment on it.
/// Only then does the BFF call FileStorage to mint the URL.
/// </summary>
public sealed class GetAttachmentReadUrlBffQueryHandler
    : AizenQueryHandler<GetAttachmentReadUrlBffQuery, AttachmentReadUrlBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetAttachmentReadUrlBffQueryHandler> _logger;

    public GetAttachmentReadUrlBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetAttachmentReadUrlBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<AttachmentReadUrlBffResponse?> Handle(
        GetAttachmentReadUrlBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        // Step 1: module access check — verifies provider relationship + fileId belongs to request
        try
        {
            var accessCheck = await _serviceRequest.CheckAttachmentAccess(request.ServiceRequestId, request.FileId);
            if (accessCheck.Body is not { Authorized: true })
                throw new AizenBusinessException("Service request not found.");
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Attachment access check failed for SR {ServiceRequestId}, file {FileId}, provider {ProfileId}",
                request.ServiceRequestId, request.FileId, _identityHolder.ProfileId);
            throw new AizenBusinessException("Service request not found.");
        }

        // Step 2: mint signed URL (short-lived, per click)
        try
        {
            var urlResponse = await _fileStorage.CreateReadUrl(
                request.FileId,
                new CreateReadUrlRequest { ExpiresIn = TimeSpan.FromMinutes(5) });

            if (urlResponse.Body is null)
                throw new AizenBusinessException("Failed to generate read URL.");

            return new AttachmentReadUrlBffResponse
            {
                Url = urlResponse.Body.ReadUrl,
                ExpiresAt = urlResponse.Body.ExpiresAt
            };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "FileStorage CreateReadUrl failed for file {FileId}", request.FileId);
            throw new AizenBusinessException("Failed to generate read URL.");
        }
    }
}
