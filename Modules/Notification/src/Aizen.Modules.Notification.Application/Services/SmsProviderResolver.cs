using Aizen.Core.Common.Abstraction.Configuration;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>Seçilecek SMS sağlayıcısı (DI'da concrete ISmsSender'e çevrilir).</summary>
public enum SmsProviderKind
{
    Stub,
    Infobip,
    Netgsm,
}

/// <summary>
/// SAF sağlayıcı seçimi: SmsOptions.Provider + sırların dolu olup olmadığına bakar. Sır boş VEYA __FROM_ENV__/
/// __FROM_SECRET__ placeholder'ı ise (AizenConfigPlaceholders.NullIfUnset null döner) sağlayıcı yapılandırılmamış
/// sayılır ve Stub'a düşülür. Böylece Development'ta (sırlar boş) sistem stub ile çalışır; sağlayıcı ancak gerçek
/// sırlar verildiğinde etkinleşir.
/// </summary>
public static class SmsProviderResolver
{
    public static SmsProviderKind Resolve(SmsOptions options)
    {
        var provider = (options.Provider ?? string.Empty).Trim().ToLowerInvariant();

        return provider switch
        {
            "infobip" when Configured(options.Infobip?.ApiKey) => SmsProviderKind.Infobip,
            "netgsm" when Configured(options.Netgsm?.UserCode) && Configured(options.Netgsm?.Password)
                => SmsProviderKind.Netgsm,
            _ => SmsProviderKind.Stub,
        };
    }

    private static bool Configured(string? secret) => AizenConfigPlaceholders.NullIfUnset(secret) is not null;
}
