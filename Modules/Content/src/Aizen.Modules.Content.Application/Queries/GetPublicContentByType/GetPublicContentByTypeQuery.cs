using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Queries.GetPublicContentByType;

/// <summary>Anonymous public feed narrowed to a single content type on a surface (§8).</summary>
public sealed class GetPublicContentByTypeQuery : AizenQuery<ContentFeedResponse>
{
    public ContentSurface Surface { get; set; }
    public ContentType Type { get; set; }
    public string Lang { get; set; } = "tr";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
