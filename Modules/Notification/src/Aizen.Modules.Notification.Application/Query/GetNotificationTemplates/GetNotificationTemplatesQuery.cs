using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationTemplates;

public sealed class GetNotificationTemplatesQuery : AizenQuery<List<NotificationTemplateDto>> { }
