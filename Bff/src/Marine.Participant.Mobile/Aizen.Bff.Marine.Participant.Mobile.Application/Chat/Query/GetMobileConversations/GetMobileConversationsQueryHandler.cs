using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

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
