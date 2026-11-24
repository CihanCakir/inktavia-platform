namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>P1 — a page of admin newsletter subscribers (newest first), mirroring the notification-history result shape.</summary>
public sealed class NewsletterSubscriberListResult
{
    public List<NewsletterSubscriberListItemDto> Items      { get; set; } = new();
    public int                                   TotalCount { get; set; }
    public int                                   Page       { get; set; }
    public int                                   PageSize   { get; set; }
}
