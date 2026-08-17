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
