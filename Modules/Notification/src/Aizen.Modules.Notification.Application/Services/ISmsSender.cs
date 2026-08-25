namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// SMS gönderim soyutlaması (vendor-agnostik). Faz 28.7'de yalnız stub uygulaması var; gerçek sağlayıcı adaptörleri
/// (Netgsm/Twilio vb.) sonraki fazın işi. IEmailSender ile aynı sınır; sonuç zengin (Success/ProviderRef/Error).
/// </summary>
public interface ISmsSender
{
    Task<SmsSendResult> SendAsync(string e164Phone, string text, CancellationToken ct);
}

/// <summary>SMS gönderim sonucu: başarı bayrağı + sağlayıcı referansı (varsa) + hata (varsa).</summary>
public sealed record SmsSendResult(bool Success, string? ProviderRef, string? Error)
{
    public static SmsSendResult Ok(string providerRef) => new(true, providerRef, null);
    public static SmsSendResult Fail(string error)     => new(false, null, error);
}
