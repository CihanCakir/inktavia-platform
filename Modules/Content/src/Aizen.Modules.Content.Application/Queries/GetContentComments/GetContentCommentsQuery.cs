using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Queries.GetContentComments;

/// <summary>Public, paged list of a content item's APPROVED comments (§8). Anonymous-readable.</summary>
public sealed class GetContentCommentsQuery : AizenQuery<ContentCommentsResponse>
{
    public string ContentId { get; set; } = default!;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
