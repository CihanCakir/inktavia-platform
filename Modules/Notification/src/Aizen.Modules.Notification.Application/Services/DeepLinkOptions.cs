namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Derin bağlantı güvenlik ayarları. "Notifications:DeepLink" bölümünden bağlanır. AllowedHosts boş/eksikse
/// yalnız '/' ile başlayan göreli yollara izin verilir (güvenli varsayılan; yeni env değişkeni gerektirmez).
/// </summary>
public sealed class DeepLinkOptions
{
    public const string SectionName = "Notifications:DeepLink";

    /// <summary>İzin verilen mutlak http/https host'ları (ör. "app.inktavia.com"). Boşsa yalnız göreli yollar geçerli.</summary>
    public List<string> AllowedHosts { get; set; } = new();
}
