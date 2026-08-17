using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// Mints a short-lived signed read URL for a service request attachment.
/// BE_WC3a two-store check: a CHAT image is authorized by the <b>Messaging</b> store first (participant +
/// fileId-on-conversation, covering old synced + new native images); on a miss it falls back to the <b>SR</b>
/// access-check (request / work-log / completion evidence, unchanged). Only then does the BFF mint the URL via FileStorage.
/// </summary>
public sealed class GetAttachmentReadUrlBffQueryHandler
    : AizenQueryHandler<GetAttachmentReadUrlBffQuery, AttachmentReadUrlBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IMessagingRemoteCall _messaging;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetAttachmentReadUrlBffQueryHandler> _logger;

    public GetAttachmentReadUrlBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        IMessagingRemoteCall messaging,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetAttachmentReadUrlBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _messaging = messaging;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<AttachmentReadUrlBffResponse?> Handle(
        GetAttachmentReadUrlBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        // Step 1: two-store access check — (1) chat image via Messaging (participant + fileId-on-conversation), then
        // (2) fall back to the SR check (request / work-log / completion evidence). Vague not-found on any mismatch.
        if (!await IsChatAttachmentAuthorizedAsync(request.ServiceRequestId, request.FileId)
            && !await IsSrAttachmentAuthorizedAsync(request.ServiceRequestId, request.FileId))
        {
            throw new AizenBusinessException("Service request not found.");
        }

        // Step 2: mint signed URL (short-lived, per click). The access check already proved the caller owns this
        // fileId — so a FileStorage failure here (e.g. a valid attachment whose backing object no longer exists)
        // is NOT a security event and must not hard-400 the render. Degrade to a clean "unavailable" (empty url)
        // the FE renders as a broken-image placeholder, instead of a 400 that spams the console on every image.
        try
        {
            var urlResponse = await _fileStorage.CreateReadUrl(
                request.FileId,
                new CreateReadUrlRequest { ExpiresIn = TimeSpan.FromMinutes(5) });

            if (urlResponse.Body is null)
            {
                _logger.LogWarning("FileStorage CreateReadUrl returned no body for file {FileId} — returning unavailable.",
                    request.FileId);
                return new AttachmentReadUrlBffResponse { Url = string.Empty };
            }

            return new AttachmentReadUrlBffResponse
            {
                Url = urlResponse.Body.ReadUrl,
                ExpiresAt = urlResponse.Body.ExpiresAt
            };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "FileStorage CreateReadUrl failed for file {FileId} — returning unavailable (object likely missing).",
                request.FileId);
            return new AttachmentReadUrlBffResponse { Url = string.Empty };
        }
    }

    private async Task<bool> IsChatAttachmentAuthorizedAsync(long serviceRequestId, Guid fileId)
    {
        try
        {
            var check = await _messaging.CheckChatAttachmentAccess(fileId, MessagingContextType.ServiceRequest, serviceRequestId);
            return check?.Body is { Authorized: true };
        }
        catch (Refit.ApiException) { return false; }
    }

    private async Task<bool> IsSrAttachmentAuthorizedAsync(long serviceRequestId, Guid fileId)
    {
        try
        {
            var check = await _serviceRequest.CheckAttachmentAccess(serviceRequestId, fileId);
            return check?.Body is { Authorized: true };
        }
        catch (Refit.ApiException) { return false; }
    }
}
