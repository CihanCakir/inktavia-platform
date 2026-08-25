using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Derin bağlantı güvenlik doğrulaması (SAF). Kabul: (a) '/' ile başlayan göreli yollar, (b) host'u allowlist'te olan
/// mutlak http/https URL'leri. Reddedilen HER ŞEY (javascript:, data:, protokol-göreli "//", bilinmeyen host, özel
/// şema) şablon kodunu belirten AizenBusinessException fırlatır. Allowlist boşsa yalnız göreli yollara izin verilir.
/// </summary>
public static class DeepLinkValidator
{
    public static void Validate(string? deepLink, IReadOnlyCollection<string> allowedHosts, string templateCode)
    {
        if (string.IsNullOrWhiteSpace(deepLink))
            return;   // derin bağlantı yok → sorun yok

        var value = deepLink.Trim();

        // Protokol-göreli ("//host/…") → reddet (scheme atlayarak dış host'a gidebilir).
        if (value.StartsWith("//", StringComparison.Ordinal))
            throw Reject(templateCode, value);

        // Göreli yol ("/…") → kabul.
        if (value.StartsWith('/'))
            return;

        // Mutlak URL: yalnız http/https + host allowlist'te ise kabul.
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            foreach (var host in allowedHosts)
            {
                if (string.Equals(host, uri.Host, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            throw Reject(templateCode, value);
        }

        // javascript:, data:, özel şema, göreli olmayan diğer her şey → reddet.
        throw Reject(templateCode, value);
    }

    private static AizenBusinessException Reject(string templateCode, string value)
        => new($"Template '{templateCode}' geçersiz derin bağlantı üretti: '{value}'. " +
               "Yalnız '/' ile başlayan göreli yollar ya da izinli http/https host'ları kabul edilir.");
}
