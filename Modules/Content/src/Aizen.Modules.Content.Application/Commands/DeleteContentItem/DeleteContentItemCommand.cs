using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.DeleteContentItem;

/// <summary>Soft-deletes a content item (§8). Elevated action. Returns the pre-delete snapshot.</summary>
public sealed class DeleteContentItemCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;
}
