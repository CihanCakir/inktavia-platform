using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

/// <summary>GET /api/v1/mobile/service-requests/{id}/messages — the owner's chat thread for one SR. Read from the
/// Messaging store by context; the module authorizes it (the caller must be a conversation participant → 403
/// otherwise), so an owner only ever reads their own thread. Cost-free; System messages ride through.</summary>
public sealed class GetMobileChatThreadQuery : AizenQuery<MobileChatThreadDto>
{
    public GetMobileChatThreadQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
    public long ServiceRequestId { get; }
}
