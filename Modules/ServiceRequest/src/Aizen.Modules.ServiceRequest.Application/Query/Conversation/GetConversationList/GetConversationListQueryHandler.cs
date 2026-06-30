using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Conversation;

[DocumentationInfo("Get conversation list query handler", "Returns all conversations mapped to summary DTOs.")]
public sealed class GetConversationListQueryHandler : AizenQueryHandler<GetConversationListQuery, GetConversationListResponse>
{
    private readonly IServiceRequestConversationRepository _repository;

    public GetConversationListQueryHandler(IServiceRequestConversationRepository repository) => _repository = repository;

    public override async Task<GetConversationListResponse> Handle(GetConversationListQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _repository.GetListAsync(request.Filter, cancellationToken);

        var items = conversations.Select(c => new ConversationSummaryDto
        {
            Id = c.Id.ToString(),
            Title = c.Title,
            Preview = c.Messages.LastOrDefault()?.Content ?? string.Empty,
            Timestamp = c.LastMessageAt,
            UnreadCount = c.UnreadCount,
            Status = c.Status,
            ServiceRequestId = c.ServiceRequestId.ToString()
        }).ToList();

        return new GetConversationListResponse(items);
    }
}
