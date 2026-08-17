using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;

namespace Aizen.Modules.Messaging.Application.Query.GetMyConversations;

[DocumentationInfo("Get my conversations query handler",
    "Returns the summaries of conversations the authenticated caller participates in, newest-message first.")]
public sealed class GetMyConversationsQueryHandler
    : AizenQueryHandler<GetMyConversationsQuery, GetConversationListResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IAizenInfoAccessor _info;

    public GetMyConversationsQueryHandler(
        IConversationRepository conversationRepository,
        IAizenInfoAccessor info)
    {
        _conversationRepository = conversationRepository;
        _info                   = info;
    }

    public override async Task<GetConversationListResponse> Handle(
        GetMyConversationsQuery request, CancellationToken cancellationToken)
    {
        // Scope = the AUTHENTICATED principal's user id (asserted by the BFF via X-Aizen-User-Id, verified by the
        // module middleware). Never a query/route parameter → no cross-participant leakage is possible.
        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        var items = await _conversationRepository.GetListForParticipantAsync(
            userId, request.ContextType, request.Skip, request.Take, cancellationToken);

        var total = await _conversationRepository.CountForParticipantAsync(
            userId, request.ContextType, cancellationToken);

        var dtos = items.Select(e => e.ToSummaryDto()).ToList();
        return new GetConversationListResponse(dtos, total);
    }
}
