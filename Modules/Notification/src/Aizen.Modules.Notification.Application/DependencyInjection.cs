using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<ITemplateInterpolator, TemplateInterpolator>();
        services.AddScoped<IFcmSender, FcmSenderStub>();
        services.AddKeyedScoped<INotificationDispatcher, InAppNotificationDispatcher>(NotificationChannel.InApp);
        services.AddKeyedScoped<INotificationDispatcher, PushNotificationDispatcher>(NotificationChannel.Push);
        services.AddScoped<INotificationDispatcher, CompositeNotificationDispatcher>();
        return services;
    }
}
