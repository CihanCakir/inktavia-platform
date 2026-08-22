using Aizen.Core.Common.Abstraction.Localization;

namespace Aizen.Modules.ReferenceData.Abstraction.Localization;

/// <summary>
/// FAZ12B #49 — Konum adını ÇAĞIRANIN DİLİNE göre çözer.
///
/// Ölçüldü (2026-08-21): seed verisi iki dilli (tr: "İstanbul", en: "Istanbul") ama beş+ noktada ad SABİT
/// "en" ile okunuyordu; Türk sağlayıcı "Muğla" beklerken "Mugla" görüyordu. Tek eksik halka: çağıranın dili.
///
/// ⚠️ `acceptLanguage` bir KÜLTÜR KODU DEĞİL, bir HTTP başlığıdır: "tr-TR,tr;q=0.9,en;q=0.8". Ham başlığı
///    doğrudan sözlük anahtarı olarak KULLANMA ve körlemesine Substring(0,2) YAPMA — en yüksek q-ağırlıklı
///    girişin birincil alt-etiketini al ("tr").
///
/// Çözüm sırası (kesin):
///   1) Çağıranın dili (normalize edilmiş birincil etiket) — sözlükte varsa.
///   2) "en" — mevcut davranış, yedek olarak korunur.
///   3) Herhangi bir değer, sonra kod — bugünkü gibi.
/// HTTP bağlamı yoksa (arka plan işi, consumer, seed) acceptLanguage null gelir → doğrudan 2. adıma.
/// </summary>
public static class LocationNameResolver
{
    public static string Resolve(
        IReadOnlyDictionary<string, string>? names,
        string fallbackCode,
        string? acceptLanguage)
    {
        if (names is null || names.Count == 0)
            return fallbackCode;

        // 1) Çağıranın dili.
        var lang = PrimaryLanguageTag(acceptLanguage);
        if (lang is not null && names.TryGetValue(lang, out var localized) && !string.IsNullOrWhiteSpace(localized))
            return localized;

        // 2) İngilizce yedek (mevcut davranış).
        if (names.TryGetValue("en", out var en) && !string.IsNullOrWhiteSpace(en))
            return en;

        // 3) Herhangi bir değer, sonra kod.
        foreach (var value in names.Values)
            if (!string.IsNullOrWhiteSpace(value))
                return value;

        return fallbackCode;
    }

    /// <summary>
    /// "tr-TR,tr;q=0.9,en;q=0.8" → "tr". FAZ13A #68: kural PAYLAŞILAN Core parser'a taşındı (BuilderMiddleware
    /// hata mesajı yerelleştirmesi de aynısını kullanıyor); burada yalnız delege ediyoruz — ikinci parser yok.
    /// </summary>
    public static string? PrimaryLanguageTag(string? acceptLanguage)
        => AcceptLanguageParser.PrimaryLanguageTag(acceptLanguage);
}
