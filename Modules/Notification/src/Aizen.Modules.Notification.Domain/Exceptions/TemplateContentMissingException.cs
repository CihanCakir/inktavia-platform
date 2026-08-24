using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Exceptions;

/// <summary>
/// Bir (templateCode, channel) için fallback zinciri (istenen locale → 'en' → ilk Published) sonunda dahi
/// yayında (Published) içerik bulunamadığında atılır. Sessizce geçmek yerine bilinçli olarak hata veriyoruz.
/// </summary>
public sealed class TemplateContentMissingException : Exception
{
    public string TemplateCode { get; }
    public NotificationChannel Channel { get; }
    public string RequestedLocale { get; }

    public TemplateContentMissingException(string templateCode, NotificationChannel channel, string requestedLocale)
        : base($"No Published template content found for TemplateCode='{templateCode}', Channel={channel}, " +
               $"requested locale='{requestedLocale}' (tried requested → 'en' → first Published).")
    {
        TemplateCode = templateCode;
        Channel = channel;
        RequestedLocale = requestedLocale;
    }
}
