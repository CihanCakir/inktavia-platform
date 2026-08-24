using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Query.PreviewTemplateContent;

public sealed class PreviewTemplateContentQuery : AizenQuery<NotificationTemplatePreviewResultDto>
{
    public string                     Code      { get; init; } = default!;
    public NotificationChannel        Channel   { get; init; }
    public string                     Locale    { get; init; } = default!;
    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();
}
