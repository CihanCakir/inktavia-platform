namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>
/// Body for scheduling a content item (§8 ScheduleContentItem): moves Draft → Scheduled with the
/// given publish window. ExpireAt is optional (open-ended when null).
/// </summary>
public sealed class ScheduleContentItemRequest
{
    public DateTimeOffset PublishAt { get; set; }
    public DateTimeOffset? ExpireAt { get; set; }
}
