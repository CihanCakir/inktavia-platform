using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>Kampanya detayı: durum + sayaçlar + yapılandırma.</summary>
public sealed class NotificationCampaignDto
{
    public long                             Id               { get; init; }
    public CampaignAudience                 Audience         { get; init; }
    public CampaignTargetMode               TargetMode       { get; init; }
    public string?                          TemplateCode     { get; init; }
    public bool                             HasCustomContent { get; init; }
    public IReadOnlyList<NotificationChannel> Channels       { get; init; } = new List<NotificationChannel>();
    public CampaignStatus                   Status           { get; init; }
    public int                              TotalRecipients  { get; init; }
    public int                              SentCount        { get; init; }
    public int                              FailedCount      { get; init; }
    public DateTimeOffset?                  ScheduledAt      { get; init; }
    public long                             CreatedByUserId  { get; init; }
    public DateTimeOffset                   CreatedAt        { get; init; }
    public DateTimeOffset?                  CompletedAt      { get; init; }
}
