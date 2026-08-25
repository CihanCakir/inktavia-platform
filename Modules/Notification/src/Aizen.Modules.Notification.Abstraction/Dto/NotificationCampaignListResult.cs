using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>Kampanya listesinde tek satır özeti.</summary>
public sealed class NotificationCampaignListItemDto
{
    public long             Id              { get; init; }
    public CampaignAudience Audience        { get; init; }
    public CampaignTargetMode TargetMode    { get; init; }
    public string?          TemplateCode    { get; init; }
    public CampaignStatus   Status          { get; init; }
    public int              TotalRecipients { get; init; }
    public int              SentCount       { get; init; }
    public int              FailedCount     { get; init; }
    public DateTimeOffset?  ScheduledAt     { get; init; }
    public DateTimeOffset   CreatedAt       { get; init; }
    public DateTimeOffset?  CompletedAt     { get; init; }
}

/// <summary>Kampanya sayfalı sonucu ({items,totalCount,page,pageSize}).</summary>
public sealed class NotificationCampaignListResult
{
    public IReadOnlyList<NotificationCampaignListItemDto> Items { get; set; } = new List<NotificationCampaignListItemDto>();
    public int TotalCount { get; set; }
    public int Page       { get; set; }
    public int PageSize   { get; set; }
}
