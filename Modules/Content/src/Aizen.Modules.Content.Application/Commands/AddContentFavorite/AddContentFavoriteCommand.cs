using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.AddContentFavorite;

/// <summary>A participant favorites a content item (§8). Idempotent — re-adding is a no-op success.</summary>
public sealed class AddContentFavoriteCommand : AizenCommand<ContentFavoriteResultDto>
{
    public string ContentId { get; set; } = default!;
}
