using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.RemoveContentFavorite;

/// <summary>A participant removes their favorite of a content item (§8). Idempotent.</summary>
public sealed class RemoveContentFavoriteCommand : AizenCommand<ContentFavoriteResultDto>
{
    public string ContentId { get; set; } = default!;
}
