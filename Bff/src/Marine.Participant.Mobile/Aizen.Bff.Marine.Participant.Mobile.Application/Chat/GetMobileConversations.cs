using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

/// <summary>GET /api/v1/mobile/conversations — the owner's chat inbox (service-request conversations; paged). Reads
/// from the Messaging store scoped to the caller by the assertion (no user id on the wire). Cost-free.</summary>
public sealed class GetMobileConversationsQuery : AizenQuery<MobileConversationListDto>
{
    public GetMobileConversationsQuery(int skip, int take)
    {
        Skip = skip < 0 ? 0 : skip;
        Take = take is <= 0 or > 100 ? 50 : take;
    }

    public int Skip { get; }
    public int Take { get; }
}

public sealed class GetMobileConversationsQueryHandler
    : AizenQueryHandler<GetMobileConversationsQuery, MobileConversationListDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IMessagingRemoteCall _messaging;

    public GetMobileConversationsQueryHandler(IParticipantProfileResolver resolver, IMessagingRemoteCall messaging)
    {
        _resolver = resolver;
        _messaging = messaging;
    }

    public override async Task<MobileConversationListDto?> Handle(
        GetMobileConversationsQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _messaging.GetMyConversations(MessagingContextType.ServiceRequest, request.Skip, request.Take);
        return MobileChatMapper.MapConversationList(resp?.Body);
    }
}
