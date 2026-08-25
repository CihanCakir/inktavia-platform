namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Telefon numarasını loglar için maskeler (KVKK). SAF. Örn: +905551234567 → +9055*****67 (ilk 5 + son 2 görünür).
/// Kısa/boş girişte güvenli davranır. Loglarda ASLA tam numara yazılmasın diye tek yer.
/// </summary>
public static class PhoneMasker
{
    public static string Mask(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "(yok)";

        var p = phone.Trim();
        if (p.Length < 8)
            // Çok kısa: yalnız son 2 hane görünür, gerisi maskeli (2 haneden kısaysa tamamen maskeli).
            return p.Length <= 2 ? new string('*', p.Length) : new string('*', p.Length - 2) + p[^2..];

        // İlk 5 + sabit 5 yıldız + son 2 (uzunluk sızdırmaz).
        return $"{p[..5]}*****{p[^2..]}";
    }
}
