using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Query;

public sealed class GetAdminNotificationTemplateByCodeQuery : AizenQuery<NotificationTemplateDto>
{
    public string Code      { get; }
    public string UserToken { get; }

    public GetAdminNotificationTemplateByCodeQuery(string code, string userToken)
    {
        Code      = code;
        UserToken = userToken;
    }
}
