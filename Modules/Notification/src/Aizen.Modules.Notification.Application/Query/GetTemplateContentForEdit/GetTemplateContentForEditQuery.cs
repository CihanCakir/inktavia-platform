using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Query.GetTemplateContentForEdit;

/// <summary>(code, channel, locale) hücresi için düzenlenebilir mevcut içeriği getirir (Draft öncelikli, yoksa Published).</summary>
public sealed class GetTemplateContentForEditQuery : AizenQuery<NotificationTemplateContentEditResult>
{
    public string              Code    { get; init; } = default!;
    public NotificationChannel Channel { get; init; }
    public string              Locale  { get; init; } = default!;
}
