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

public sealed class GetMobileChatThreadQueryHandler
    : AizenQueryHandler<GetMobileChatThreadQuery, MobileChatThreadDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IMessagingRemoteCall _messaging;

    public GetMobileChatThreadQueryHandler(IParticipantProfileResolver resolver, IMessagingRemoteCall messaging)
    {
        _resolver = resolver;
        _messaging = messaging;
    }

    public override async Task<MobileChatThreadDto?> Handle(
        GetMobileChatThreadQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _messaging.GetMyConversationByContext(MessagingContextType.ServiceRequest, request.ServiceRequestId);
        var thread = MobileChatMapper.MapThread(resp?.Body);
        // Preserve the SR id even when the conversation doesn't exist yet (empty thread for a fresh SR).
        if (thread.ServiceRequestId <= 0) thread.ServiceRequestId = request.ServiceRequestId;
        return thread;
    }
}
