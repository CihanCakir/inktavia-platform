using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Application.Mapping;

/// <summary>Content entity → DTO eşlemesi (handler'lar arasında tekrar etmemek için).</summary>
public static class TemplateContentMapping
{
    public static NotificationTemplateContentDto ToDto(this NotificationTemplateContentEntity c) => new()
    {
        Id               = c.Id,
        TemplateId       = c.TemplateId,
        Channel          = c.Channel,
        Locale           = c.Locale,
        Version          = c.Version,
        Status           = c.Status,
        SubjectTemplate  = c.SubjectTemplate,
        HtmlTemplate     = c.HtmlTemplate,
        TextTemplate     = c.TextTemplate,
        LayoutCode       = c.LayoutCode,
        TitleTemplate    = c.TitleTemplate,
        BodyTemplate     = c.BodyTemplate,
        DeepLinkTemplate = c.DeepLinkTemplate,
        SmsTextTemplate  = c.SmsTextTemplate,
        CreatedAt        = c.CreatedAt,
        UpdatedAt        = c.UpdatedAt,
    };

    public static NotificationTemplateVersionDto ToVersionDto(this NotificationTemplateContentEntity c) => new()
    {
        Id        = c.Id,
        Version   = c.Version,
        Status    = c.Status,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
    };
}
