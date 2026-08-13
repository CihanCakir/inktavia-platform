using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.ArchiveContentItem;

/// <summary>Archives a content item (§8). Elevated action.</summary>
public sealed class ArchiveContentItemCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;
}
