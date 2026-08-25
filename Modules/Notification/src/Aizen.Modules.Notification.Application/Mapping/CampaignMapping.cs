using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Application.Mapping;

/// <summary>Campaign entity → DTO eşlemesi (ChannelsCsv → NotificationChannel listesi burada çözülür).</summary>
public static class CampaignMapping
{
    public static IReadOnlyList<NotificationChannel> ParseChannels(string csv)
        => csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Select(s => (NotificationChannel)int.Parse(s))
              .Distinct()
              .ToList();

    public static NotificationCampaignDto ToDto(this NotificationCampaignEntity c) => new()
    {
        Id               = c.Id,
        Audience         = c.Audience,
        TargetMode       = c.TargetMode,
        TemplateCode     = c.TemplateCode,
        HasCustomContent = !string.IsNullOrWhiteSpace(c.CustomContentJson),
        Channels         = ParseChannels(c.ChannelsCsv),
        Status           = c.Status,
        TotalRecipients  = c.TotalRecipients,
        SentCount        = c.SentCount,
        FailedCount      = c.FailedCount,
        ScheduledAt      = c.ScheduledAt,
        CreatedByUserId  = c.CreatedByUserId,
        CreatedAt        = c.CreatedAt,
        CompletedAt      = c.CompletedAt,
    };

    public static NotificationCampaignListItemDto ToListItemDto(this NotificationCampaignEntity c) => new()
    {
        Id              = c.Id,
        Audience        = c.Audience,
        TargetMode      = c.TargetMode,
        TemplateCode    = c.TemplateCode,
        Status          = c.Status,
        TotalRecipients = c.TotalRecipients,
        SentCount       = c.SentCount,
        FailedCount     = c.FailedCount,
        ScheduledAt     = c.ScheduledAt,
        CreatedAt       = c.CreatedAt,
        CompletedAt     = c.CompletedAt,
    };
}
