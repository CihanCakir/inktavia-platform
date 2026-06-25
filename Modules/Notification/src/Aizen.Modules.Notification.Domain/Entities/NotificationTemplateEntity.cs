using Aizen.Core.Domain;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

public sealed class NotificationTemplateEntity : AizenEntity
{
    public string              TemplateCode  { get; private set; } = default!;
    public string              Name          { get; private set; } = default!;
    public NotificationType    Type          { get; private set; }
    public NotificationChannel Channel       { get; private set; }
    public string              TitleTemplate { get; private set; } = default!;
    public string              BodyTemplate  { get; private set; } = default!;
    public bool                IsActive      { get; private set; }
    public DateTimeOffset      CreatedAt     { get; private set; }
    public DateTimeOffset?     UpdatedAt     { get; private set; }

    private NotificationTemplateEntity() { }

    public static NotificationTemplateEntity Create(
        string templateCode,
        string name,
        NotificationType type,
        NotificationChannel channel,
        string titleTemplate,
        string bodyTemplate)
    {
        return new NotificationTemplateEntity
        {
            TemplateCode  = templateCode.ToUpperInvariant(),
            Name          = name,
            Type          = type,
            Channel       = channel,
            TitleTemplate = titleTemplate,
            BodyTemplate  = bodyTemplate,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string name, string titleTemplate, string bodyTemplate)
    {
        Name          = name;
        TitleTemplate = titleTemplate;
        BodyTemplate  = bodyTemplate;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive  = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
