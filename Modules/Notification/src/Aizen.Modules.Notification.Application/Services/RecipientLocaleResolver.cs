using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Alıcı locale'ini tek noktadan çözer. Öncelik: (1) kalıcı tercih → (2) istek bağlamı → (3) varsayılan.
/// E-posta alıcısı çözümüyle AYNI modül sınırını kullanır: UserProfiles→Users bakışı Identity tarafında yaşar,
/// buraya yalnızca bir HTTP remote-call ile ulaşılır (best-effort — bir aksaklık bir sonraki adıma düşürür).
/// Kod doğrulaması konfigürasyondan yüklenen sabit allowlist'e göre yapılır; çözüm başına ReferenceData'ya GİDİLMEZ.
/// </summary>
public sealed class RecipientLocaleResolver : ILocaleResolver
{
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly IAizenClientInfoAccessor _clientInfo;
    private readonly NotificationOptions _options;
    private readonly ILogger<RecipientLocaleResolver> _logger;

    public RecipientLocaleResolver(
        INotificationIdentityRemoteCall identity,
        IAizenClientInfoAccessor clientInfo,
        IOptions<NotificationOptions> options,
        ILogger<RecipientLocaleResolver> logger)
    {
        _identity = identity;
        _clientInfo = clientInfo;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ResolveAsync(long recipientProfileOrUserId, CancellationToken ct = default)
    {
        // (1) Kalıcı tercih — Users.PreferredLanguage (e-posta çözümleyicisiyle aynı UserProfiles→Users bakışı).
        //     Remote-call best-effort: bir hata/parse sorunu sessizce bir sonraki adıma düşürür.
        if (recipientProfileOrUserId > 0)
        {
            try
            {
                var resolved = await _identity.GetProfilePreferredLanguage(recipientProfileOrUserId);
                var persisted = NormalizeAndValidate(resolved?.Body?.PreferredLanguage);
                if (persisted is not null)
                    return persisted;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Locale resolve via Identity failed for recipient={Rid}; falling back to request/default.",
                    recipientProfileOrUserId);
            }
        }

        // (2) İstek bağlamı — ClientInfo.Language ham Accept-Language olabilir; normalize + allowlist ile doğrulanır.
        var fromRequest = NormalizeAndValidate(_clientInfo.ClientInfo?.Language);
        if (fromRequest is not null)
            return fromRequest;

        // (3) Son çare — konfigürasyondaki varsayılan locale.
        return _options.DefaultLocale;
    }

    /// <summary>
    /// Ham bir kodu ("en-US,en;q=0.9", "TR", "tr-TR" vb.) bölge-siz küçük harfe indirger ve allowlist'e göre doğrular.
    /// Allowlist'te yoksa null döner (çağıran bir üst kaynağa düşer). Hiçbir dış çağrı yapmaz.
    /// </summary>
    private string? NormalizeAndValidate(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        // Accept-Language listesi/q-değerini ve bölge ekini ("tr-TR") at → çıplak dil kodu.
        var first = candidate.Split(',', ';')[0];
        var code = first.Split('-')[0].Trim().ToLowerInvariant();
        if (code.Length == 0)
            return null;

        foreach (var allowed in _options.AllowedLocales)
        {
            if (!string.IsNullOrWhiteSpace(allowed) &&
                string.Equals(allowed.Trim(), code, StringComparison.OrdinalIgnoreCase))
                return code;
        }

        return null;
    }
}
