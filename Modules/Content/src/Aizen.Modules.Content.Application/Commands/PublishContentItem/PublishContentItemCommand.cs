using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.PublishContentItem;

/// <summary>Publishes a content item (§8). Elevated action — Admin/SuperAdmin/ContentAdmin only.</summary>
public sealed class PublishContentItemCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;
}
