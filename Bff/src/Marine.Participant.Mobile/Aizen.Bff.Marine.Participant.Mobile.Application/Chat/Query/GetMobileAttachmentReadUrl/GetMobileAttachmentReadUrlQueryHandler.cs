using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

public sealed class GetMobileAttachmentReadUrlQueryHandler
    : AizenQueryHandler<GetMobileAttachmentReadUrlQuery, MobileAttachmentReadUrlDto>
{
    private static readonly TimeSpan ReadUrlTtl = TimeSpan.FromMinutes(5);

    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IMessagingRemoteCall _messaging;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetMobileAttachmentReadUrlQueryHandler> _logger;

    public GetMobileAttachmentReadUrlQueryHandler(
        IParticipantProfileResolver resolver,
        IServiceRequestRemoteCall sr,
        IMessagingRemoteCall messaging,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetMobileAttachmentReadUrlQueryHandler> logger)
    {
        _resolver = resolver;
        _sr = sr;
        _messaging = messaging;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileAttachmentReadUrlDto?> Handle(
        GetMobileAttachmentReadUrlQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // BE_WC3a two-store check. (1) CHAT image → Messaging (participant + fileId-on-conversation). (2) On a miss →
        // the SR access-check (request / work-log / completion evidence). A vague not-found on any mismatch (no leak).
        if (!await IsChatAttachmentAuthorizedAsync(request.ServiceRequestId, request.FileId)
            && !await IsSrAttachmentAuthorizedAsync(request.ServiceRequestId, request.FileId))
        {
            throw new AizenBusinessException("Attachment not found.");
        }

        // Authorized → mint the presigned GET. A resolved-but-missing object → empty url (FE placeholder).
        try
        {
            var url = await _fileStorage.CreateReadUrl(request.FileId, new CreateReadUrlRequest { ExpiresIn = ReadUrlTtl });
            if (url?.Body is null)
                return new MobileAttachmentReadUrlDto { Url = string.Empty };

            return new MobileAttachmentReadUrlDto { Url = url.Body.ReadUrl, ExpiresAt = url.Body.ExpiresAt };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not mint a read-url for chat attachment {FileId} on SR {SrId}.",
                request.FileId, request.ServiceRequestId);
            return new MobileAttachmentReadUrlDto { Url = string.Empty };
        }
    }

    private async Task<bool> IsChatAttachmentAuthorizedAsync(long serviceRequestId, Guid fileId)
    {
        try
        {
            var check = await _messaging.CheckChatAttachmentAccess(fileId, MessagingContextType.ServiceRequest, serviceRequestId);
            return check?.Body is { Authorized: true };
        }
        catch (ApiException) { return false; }
    }

    private async Task<bool> IsSrAttachmentAuthorizedAsync(long serviceRequestId, Guid fileId)
    {
        try
        {
            var check = await _sr.CheckAttachmentAccess(serviceRequestId, fileId);
            return check?.Body is { Authorized: true };
        }
        catch (ApiException) { return false; }
    }
}
