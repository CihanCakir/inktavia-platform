using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Query.GetModerationQueue;

[DocumentationInfo("Get moderation queue query", "Returns flagged and pending-review messages for admin review.")]
public sealed class GetModerationQueueQuery : AizenQuery<GetModerationQueueResponse>
{
    public int Skip { get; }
    public int Take { get; }

    public GetModerationQueueQuery(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }
}
