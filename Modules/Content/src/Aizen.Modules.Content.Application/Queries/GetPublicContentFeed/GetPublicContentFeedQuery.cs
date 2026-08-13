using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Queries.GetPublicContentFeed;

/// <summary>
/// Anonymous public feed for a surface (§8). Serves only Published, in-window, Public-audience items
/// placed on the requested surface, localized to <see cref="Lang"/> (falling back to DefaultLanguage).
/// </summary>
public sealed class GetPublicContentFeedQuery : AizenQuery<ContentFeedResponse>
{
    public ContentSurface Surface { get; set; }
    public string Lang { get; set; } = "tr";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
