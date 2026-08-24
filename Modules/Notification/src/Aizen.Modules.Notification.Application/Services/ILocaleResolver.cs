namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Bir bildirim alıcısı için tek locale çözümleme noktası. Öncelik sırası:
/// (1) Alıcının kalıcı tercihi (Users.PreferredLanguage — e-posta çözümleyicisiyle aynı UserProfiles→Users bakışı),
/// (2) İstek bağlamındaki ClientInfo.Language (geçerli bir koda normalize edilebiliyorsa),
/// (3) NotificationOptions.DefaultLocale.
/// Bu faz yalnızca çözümleyiciyi ekler; şablon render'a HENÜZ bağlanmaz.
/// </summary>
public interface ILocaleResolver
{
    /// <param name="recipientProfileOrUserId">
    /// UserProfiles.Id (participant/provider profili) — e-posta çözümleyicisinin kullandığı anahtarla aynı.
    /// </param>
    Task<string> ResolveAsync(long recipientProfileOrUserId, CancellationToken ct = default);
}
