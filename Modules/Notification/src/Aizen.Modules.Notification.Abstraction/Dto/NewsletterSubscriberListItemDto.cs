using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>P1 — one admin-facing subscriber row. The email is visible to admins (Admin/SuperAdmin only); the
/// confirmation/unsubscribe tokens and IP hash are internal and never leave the module.</summary>
public sealed class NewsletterSubscriberListItemDto
{
    public long                       Id             { get; set; }
    public string                     Email          { get; set; } = default!;
    public string                     Lang           { get; set; } = default!;
    public NewsletterSubscriberStatus Status         { get; set; }
    public string?                    SourcePage     { get; set; }
    public int                        SpamScore      { get; set; }
    public DateTimeOffset?            ConfirmedAt    { get; set; }
    public DateTimeOffset?            UnsubscribedAt { get; set; }
    public DateTimeOffset             CreatedAt      { get; set; }
}
