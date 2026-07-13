using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplicationServices(
        this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<ITemplateInterpolator, TemplateInterpolator>();
        services.AddScoped<IFcmSender, FcmSenderStub>();
        services.AddScoped<IPushSender, WebPushSender>();

        // VAPID configuration for Web Push
        if (configuration is not null)
        {
            services.Configure<VapidOptions>(configuration.GetSection(VapidOptions.SectionName));
        }

        // Email sender: use SMTP when configured, otherwise a logging stub
        if (configuration is not null)
        {
            services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        }

        var smtpHost = configuration?.GetValue<string>("Email:Host");
        if (!string.IsNullOrWhiteSpace(smtpHost))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, LoggingEmailSenderStub>();

        services.AddKeyedScoped<INotificationDispatcher, InAppNotificationDispatcher>(NotificationChannel.InApp);
        services.AddKeyedScoped<INotificationDispatcher, PushNotificationDispatcher>(NotificationChannel.Push);
        services.AddKeyedScoped<INotificationDispatcher, EmailNotificationDispatcher>(NotificationChannel.Email);
        services.AddScoped<INotificationDispatcher, CompositeNotificationDispatcher>();
        return services;
    }
}
