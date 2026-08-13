using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.UnpublishContentItem;

/// <summary>Unpublishes a content item (Published → Draft, §8). Elevated action.</summary>
public sealed class UnpublishContentItemCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;
}
