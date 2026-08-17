using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;

namespace Aizen.Modules.Messaging.Application.Query.GetModerationQueue;

[DocumentationInfo("Get moderation queue query handler",
    "Returns messages flagged for human review from the moderation queue.")]
public sealed class GetModerationQueueQueryHandler
    : AizenQueryHandler<GetModerationQueueQuery, GetModerationQueueResponse>
{
    private readonly IConversationMessageRepository _messageRepository;

    public GetModerationQueueQueryHandler(IConversationMessageRepository messageRepository)
        => _messageRepository = messageRepository;

    public override async Task<GetModerationQueueResponse> Handle(
        GetModerationQueueQuery request, CancellationToken cancellationToken)
    {
        var items = await _messageRepository.GetFlaggedAsync(request.Skip, request.Take, cancellationToken);
        var dtos  = items.Select(m => m.ToModerationDto()).ToList();
        return new GetModerationQueueResponse(dtos, dtos.Count);
    }
}
