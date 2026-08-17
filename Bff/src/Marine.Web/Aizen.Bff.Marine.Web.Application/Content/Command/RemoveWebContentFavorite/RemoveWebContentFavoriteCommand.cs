using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Bff.Marine.Web.Application.Content.Command.RemoveWebContentFavorite;

/// <summary>DELETE /api/v1/web/me/content/items/{id}/favorite — remove the participant's favorite (idempotent).</summary>
public sealed class RemoveWebContentFavoriteCommand : AizenCommand<ContentFavoriteResultDto>
{
    public string ContentId { get; set; } = default!;
}
