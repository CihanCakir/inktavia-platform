namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// SMS ayarları (EmailOptions ile aynı şekilde configuration'dan bağlanır). Sağlayıcı seçimi Provider ile yapılır;
/// sır taşıyan alanlar appsettings'te __FROM_ENV__/__FROM_SECRET__ placeholder'ıyla yazılır (FAZ14 guard korur).
/// Development'ta boş/placeholder → sağlayıcı stub'a düşer (uyarı loglanır). Bkz. AizenConfigPlaceholders.NullIfUnset.
/// </summary>
public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>Sağlayıcı: "stub" | "infobip" | "netgsm" (varsayılan "stub"). Sır boş/placeholder ise stub'a düşülür.</summary>
    public string Provider { get; set; } = "stub";

    /// <summary>Baştaki 0'lı ulusal numaralara uygulanacak varsayılan ülke kodu (E.164 normalizasyonu).</summary>
    public string DefaultCountryCode { get; set; } = "+90";

    public InfobipSmsOptions Infobip { get; set; } = new();
    public NetgsmSmsOptions  Netgsm  { get; set; } = new();
}

/// <summary>Infobip ayarları. ApiKey sır; BaseUrl/From ortam-değişkeni.</summary>
public sealed class InfobipSmsOptions
{
    public string? BaseUrl { get; set; }
    public string? ApiKey  { get; set; }
    /// <summary>Gönderen (alfanümerik sender id ya da numara).</summary>
    public string? From    { get; set; }
}

/// <summary>Netgsm ayarları. Password sır; UserCode/MsgHeader ortam-değişkeni.</summary>
public sealed class NetgsmSmsOptions
{
    public string  BaseUrl   { get; set; } = "https://api.netgsm.com.tr";
    public string? UserCode  { get; set; }
    public string? Password  { get; set; }
    /// <summary>Onaylı başlık (sender id) — Netgsm hesabında tanımlı olmalı.</summary>
    public string? MsgHeader { get; set; }
}
