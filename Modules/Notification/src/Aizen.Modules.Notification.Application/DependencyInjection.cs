using System;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Application.Services.Firebase;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplicationServices(
        this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<ITemplateInterpolator, TemplateInterpolator>();
        // Üretim + (ileride) preview'in paylaştığı tek strict renderer.
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();
        services.AddScoped<IPushSender, WebPushSender>();

        // M4 — contact-intake spam thresholds (config-driven; IpHashSalt from secret).
        if (configuration is not null)
            services.Configure<Command.SubmitContactMessage.ContactIntakeOptions>(
                configuration.GetSection(Command.SubmitContactMessage.ContactIntakeOptions.SectionName));

        // FCM sender: the real FirebaseAdmin FcmSender when the Firebase service account is configured
        // (ProjectId + PrivateKey + ClientEmail present, injected via env/secret), otherwise the dev logging stub —
        // so the module builds/runs/tests with no credentials. The stub is retained as the no-creds fallback.
        if (configuration is not null)
            services.Configure<PushFirebaseSettings>(configuration.GetSection(PushFirebaseSettings.SectionName));

        var firebase = configuration?.GetSection(PushFirebaseSettings.SectionName).Get<PushFirebaseSettings>();
        if (firebase?.IsConfigured == true)
            services.AddScoped<IFcmSender, FcmSender>();
        else
            services.AddScoped<IFcmSender, FcmSenderStub>();

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

        // Locale — alıcı dili çözümleme ayarları (DefaultLocale + allowlist) EmailOptions ile aynı şekilde bağlanır.
        if (configuration is not null)
        {
            services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));
        }

        // SMS ayarları (EmailOptions ile aynı şekilde configuration'dan bağlanır).
        if (configuration is not null)
        {
            services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
        }

        // Faz 28.8 — derin bağlantı host allowlist'i ("Notifications:DeepLink"). Bölüm yoksa allowlist boş (göreli-yalnız).
        if (configuration is not null)
        {
            services.Configure<DeepLinkOptions>(configuration.GetSection(DeepLinkOptions.SectionName));
        }

        // Locale çözümleyici: kalıcı tercih → istek bağlamı → varsayılan. Diğer servislerle aynı Scoped ömür.
        services.AddScoped<ILocaleResolver, RecipientLocaleResolver>();

        var smtpHost = configuration?.GetValue<string>("Email:Host");
        if (!string.IsNullOrWhiteSpace(smtpHost))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, LoggingEmailSenderStub>();

        // SMS gönderici: Sms:Provider'a göre startup'ta seçilir (stub | infobip | netgsm). Sırlar boş/placeholder ise
        // (Development) sağlayıcı stub'a düşer ve uyarı loglanır. HTTP adaptörleri IHttpClientFactory kullanır.
        // Faz 28.8 — adaptörler CreateClient(nameof(...)) ile isimli client çeker; timeout 15 sn (varsayılan 100 sn değil).
        services.AddHttpClient(nameof(InfobipSmsSender), c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient(nameof(NetgsmSmsSender),  c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<ISmsSender>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<SmsOptions>>().Value;
            var kind = SmsProviderResolver.Resolve(opts);

            var requested = (opts.Provider ?? "stub").Trim().ToLowerInvariant();
            if (kind == SmsProviderKind.Stub && requested is "infobip" or "netgsm")
            {
                sp.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("SmsSenderSelection")
                    .LogWarning("SMS sağlayıcı '{Provider}' seçildi ama sırlar boş/placeholder → stub'a düşülüyor.", requested);
            }

            return kind switch
            {
                SmsProviderKind.Infobip => ActivatorUtilities.CreateInstance<InfobipSmsSender>(sp),
                SmsProviderKind.Netgsm  => ActivatorUtilities.CreateInstance<NetgsmSmsSender>(sp),
                _                        => ActivatorUtilities.CreateInstance<LoggingSmsSenderStub>(sp),
            };
        });

        services.AddKeyedScoped<INotificationDispatcher, InAppNotificationDispatcher>(NotificationChannel.InApp);
        services.AddKeyedScoped<INotificationDispatcher, PushNotificationDispatcher>(NotificationChannel.Push);
        services.AddKeyedScoped<INotificationDispatcher, EmailNotificationDispatcher>(NotificationChannel.Email);
        services.AddKeyedScoped<INotificationDispatcher, SmsNotificationDispatcher>(NotificationChannel.Sms);
        services.AddScoped<INotificationDispatcher, CompositeNotificationDispatcher>();

        // Faz 28.6 — admin kampanya dağıtımı: alıcı genişletme (Identity iç uçları) + dağıtım servisi.
        services.AddScoped<ICampaignRecipientExpander, IdentityCampaignRecipientExpander>();
        services.AddScoped<ICampaignDispatchService, CampaignDispatchService>();
        return services;
    }
}
