using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Bff.Marine.Web.Application.Content.Command.AddWebContentFavorite;

/// <summary>POST /api/v1/web/me/content/items/{id}/favorite — favorite a content item (idempotent).</summary>
public sealed class AddWebContentFavoriteCommand : AizenCommand<ContentFavoriteResultDto>
{
    public string ContentId { get; set; } = default!;
}
