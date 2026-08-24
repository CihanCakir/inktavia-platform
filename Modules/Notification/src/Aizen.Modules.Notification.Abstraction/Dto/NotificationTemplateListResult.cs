namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>Admin template listesinin sayfalama zarfı ({items,totalCount,page,pageSize}). AdminFileListResult ile aynı şekil.</summary>
public sealed class NotificationTemplateListResult
{
    public IReadOnlyList<NotificationTemplateListItemDto> Items { get; set; } = new List<NotificationTemplateListItemDto>();
    public int TotalCount { get; set; }
    public int Page       { get; set; }
    public int PageSize   { get; set; }
}
