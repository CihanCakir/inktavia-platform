using System.Globalization;

namespace Aizen.Core.Common.Abstraction.Localization;

/// <summary>
/// FAZ13A #68 — PAYLAŞILAN Accept-Language ayrıştırıcısı.
///
/// Faz 12B'de aynı kural ReferenceData'daki LocationNameResolver içinde yazılmıştı; artık hem konum-adı
/// yerelleştirmesi (Modül) hem de hata-mesajı yerelleştirmesi (Core/Api BuilderMiddleware) aynı kuralı
/// kullandığından, İKİNCİ bir ayrıştırıcı yazmamak için buraya (her ikisinin de referans verdiği Core.Common
/// tabanına) taşındı. LocationNameResolver artık buraya delege eder.
///
/// ⚠️ Accept-Language bir KÜLTÜR KODU DEĞİL, bir HTTP başlığıdır: "tr-TR,tr;q=0.9,en;q=0.8". Ham başlığı
///    doğrudan sözlük anahtarı olarak kullanma, kör Substring(0,2) yapma — en yüksek q-ağırlıklı girişin
///    birincil alt-etiketini al ("tr"). q belirtilmeyen girişin ağırlığı 1.0'dır (RFC 7231).
/// </summary>
public static class AcceptLanguageParser
{
    /// <summary>
    /// "tr-TR,tr;q=0.9,en;q=0.8" → "tr". Boş/bozuk/"*" → null (çağıran kendi yedeğine düşer).
    /// </summary>
    public static string? PrimaryLanguageTag(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return null;

        string? bestTag = null;
        var bestWeight = double.NegativeInfinity;

        foreach (var rawPart in acceptLanguage.Split(','))
        {
            var part = rawPart.Trim();
            if (part.Length == 0) continue;

            var segments = part.Split(';');
            var tag = segments[0].Trim();
            if (tag.Length == 0 || tag == "*") continue;

            var weight = 1.0;
            for (var i = 1; i < segments.Length; i++)
            {
                var seg = segments[i].Trim();
                if (seg.StartsWith("q=", StringComparison.OrdinalIgnoreCase) &&
                    double.TryParse(seg.AsSpan(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var q))
                    weight = q;
            }

            if (weight > bestWeight)
            {
                bestWeight = weight;
                bestTag = tag;
            }
        }

        if (bestTag is null) return null;

        var primary = bestTag.Split('-')[0].Trim().ToLowerInvariant();
        return primary.Length == 0 ? null : primary;
    }
}
