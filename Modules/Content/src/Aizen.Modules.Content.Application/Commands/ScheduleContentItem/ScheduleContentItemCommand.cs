using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.ScheduleContentItem;

/// <summary>Schedules a content item: Draft/Scheduled → Scheduled with the given publish window (§8).</summary>
public sealed class ScheduleContentItemCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;
    public DateTimeOffset PublishAt { get; set; }
    public DateTimeOffset? ExpireAt { get; set; }
}
