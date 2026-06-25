using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;

namespace Aizen.Modules.Messaging.Application.Query.GetConversationList;

[DocumentationInfo("Get conversation list query handler", "Returns paginated conversation summaries with unread counts.")]
public sealed class GetConversationListQueryHandler
    : AizenQueryHandler<GetConversationListQuery, GetConversationListResponse>
{
    private readonly IConversationRepository _conversationRepository;

    public GetConversationListQueryHandler(IConversationRepository conversationRepository)
        => _conversationRepository = conversationRepository;

    public override async Task<GetConversationListResponse> Handle(
        GetConversationListQuery request, CancellationToken cancellationToken)
    {
        var items = await _conversationRepository.GetListAsync(
            request.Status, request.ContextType, request.Skip, request.Take, cancellationToken);

        var total = await _conversationRepository.CountAsync(
            request.Status, request.ContextType, cancellationToken);

        var dtos = items.Select(e => e.ToSummaryDto()).ToList();
        return new GetConversationListResponse(dtos, total);
    }
}
