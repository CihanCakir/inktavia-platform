using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Application.Mapping;

/// <summary>P1 — newsletter subscriber entity → admin DTO. Tokens and IP hash are internal and never mapped out.</summary>
public static class NewsletterSubscriberMapping
{
    public static NewsletterSubscriberListItemDto ToListItemDto(this NewsletterSubscriberEntity s) => new()
    {
        Id             = s.Id,
        Email          = s.Email,
        Lang           = s.Lang,
        Status         = s.Status,
        SourcePage     = s.SourcePage,
        SpamScore      = s.SpamScore,
        ConfirmedAt    = s.ConfirmedAt,
        UnsubscribedAt = s.UnsubscribedAt,
        CreatedAt      = s.CreatedAt,
    };
}
