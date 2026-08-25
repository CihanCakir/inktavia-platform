using System.Linq;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Telefon numarasını E.164'e normalize eden SAF yardımcı (boşluk/tire/parantez temizler; 00 → +; baştaki 0'lı ulusal
/// numaraya DefaultCountryCode uygular; +'lı ya da 0'sız numarayı olduğu gibi uluslararası kabul eder). Yan etkisiz,
/// birim-test edilir. Rakam yoksa null döner.
/// </summary>
public static class PhoneNumberNormalizer
{
    public static string? ToE164(string? raw, string defaultCountryCode)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        var hadPlus = trimmed.StartsWith('+');
        var digits  = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;

        // Ülke kodunu daima "+<rakamlar>" biçimine getir (ayarda '+' unutulmuş olabilir).
        var ccDigits = new string((defaultCountryCode ?? string.Empty).Where(char.IsDigit).ToArray());
        var cc = "+" + ccDigits;

        if (hadPlus)
            return "+" + digits;                        // zaten E.164 (yalnız ayraçları temizlenir)
        if (digits.StartsWith("00"))
            return "+" + digits.Substring(2);           // uluslararası 00 öneki → +
        if (digits.StartsWith("0"))
            return cc + digits.Substring(1);            // baştaki 0'lı ulusal → varsayılan ülke kodu
        return "+" + digits;                            // +'sız, 0'sız → zaten ülke kodunu içerdiği varsayılır
    }
}
