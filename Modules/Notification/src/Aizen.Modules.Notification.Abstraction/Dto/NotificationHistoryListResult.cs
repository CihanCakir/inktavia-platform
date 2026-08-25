namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>Admin gönderim geçmişi sayfalı sonucu (NotificationTemplateListResult ile aynı {items,totalCount,page,pageSize} şekli).</summary>
public sealed class NotificationHistoryListResult
{
    public IReadOnlyList<NotificationHistoryListItemDto> Items { get; set; } = new List<NotificationHistoryListItemDto>();
    public int TotalCount { get; set; }
    public int Page       { get; set; }
    public int PageSize   { get; set; }
}
