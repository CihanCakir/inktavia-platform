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

/// <summary>GET /api/v1/mobile/service-requests/{id}/attachments/{fileId}/read-url — a short-lived signed read-url for
/// a chat image on the owner's own SR (BE_MO10b). BE_WC3a two-store check: the CHAT image is authorized by the
/// <b>Messaging</b> store first (participant + fileId-on-conversation, covering old synced + new native images); on a
/// miss it falls back to the <b>SR</b> access-check (request / work-log / completion evidence, unchanged). Either way the
/// BFF mints the presigned GET via FileStorage. A failed access-check is a clean not-found; a resolved-but-missing object
/// degrades to an empty url (FE placeholder — not a security event, the access-check already proved ownership).</summary>
public sealed class GetMobileAttachmentReadUrlQuery : AizenQuery<MobileAttachmentReadUrlDto>
{
    public GetMobileAttachmentReadUrlQuery(long serviceRequestId, Guid fileId)
    {
        ServiceRequestId = serviceRequestId;
        FileId = fileId;
    }

    public long ServiceRequestId { get; }
    public Guid FileId { get; }
}

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
