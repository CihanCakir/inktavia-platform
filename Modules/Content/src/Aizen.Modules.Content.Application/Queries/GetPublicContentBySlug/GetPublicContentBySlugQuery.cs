using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Queries.GetPublicContentBySlug;

/// <summary>
/// Anonymous public detail by slug (§8). Returns the full item only when it is Published, in its
/// publish window and Public-audience; otherwise null. The detail carries all translations; Lang is
/// used only for cache-key parity.
/// </summary>
public sealed class GetPublicContentBySlugQuery : AizenQuery<ContentItemDto?>
{
    public string Slug { get; set; } = default!;
    public string Lang { get; set; } = "tr";
}
