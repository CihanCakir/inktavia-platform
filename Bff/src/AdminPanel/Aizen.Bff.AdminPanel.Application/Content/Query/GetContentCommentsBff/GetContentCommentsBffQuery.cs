using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Content.Query;

/// <summary>
/// Comment moderation queue for a single content item. The module's queue is per-content-item
/// (ContentId is required); there is no global cross-item pending queue. Status narrows the queue
/// (e.g. Pending) and is forwarded verbatim.
/// </summary>
public sealed class GetContentCommentsBffQuery : AizenQuery<ContentCommentsBffResult>
{
    public string ContentId { get; init; } = default!;
    public ContentCommentStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
