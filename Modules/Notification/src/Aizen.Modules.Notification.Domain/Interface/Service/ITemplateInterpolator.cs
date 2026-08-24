namespace Aizen.Modules.Notification.Domain.Interface.Service;

public interface ITemplateInterpolator
{
    /// <summary>Esnek: bilinmeyen {{key}} olduğu gibi bırakılır (geriye dönük uyumluluk).</summary>
    string Interpolate(string template, IReadOnlyDictionary<string, string> variables);

    /// <summary>
    /// STRICT: şablondaki her {{key}} variables'ta bulunmalı; eksik olan(lar) varsa eksik anahtar listesini döndürür
    /// (out missingKeys) ve sonucu üretmez. eski davranış sessizce bırakıyordu — bilerek sıkılaştırıldı.
    /// </summary>
    /// <returns>Tüm anahtarlar mevcutsa render edilmiş metin; aksi halde null (missingKeys doldurulur).</returns>
    string? TryInterpolateStrict(
        string template,
        IReadOnlyDictionary<string, string> variables,
        out IReadOnlyList<string> missingKeys);
}
