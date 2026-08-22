namespace Aizen.Core.Common.Abstraction.Localization;

/// <summary>
/// FAZ13A #68 — hata mesajı dilini GERÇEKTEN beslenen kanaldan çözer.
///
/// Ölçüm (2026-08-21): eski BuilderMiddleware yalnız "Language" başlığına bakıyordu; onu ne frontend ne de
/// BFF gönderiyor → dil hep "TR" kalıyordu. Konum adları ise Accept-Language ile yerelleşiyordu (iki kanal,
/// biri besleniyor, öteki asla). Bu çözümleyici hata kanalını, beslenen kanaldan doldurur.
///
/// Sıra:
///   1) Accept-Language (frontend + tüm BFF'ler gönderiyor) — AcceptLanguageParser ile birincil etiket ("tr").
///   2) Legacy "Language" başlığı (gönderen bir şey varsa çalışsın).
///   3) "TR" (bugünkü varsayılan). Bozuk/boş Accept-Language → parser null → sonraki adıma.
///
/// NOT: sözlükte KARŞILIĞI OLMAYAN bir dile çözülmesi sorun değil — AizenException.GetErrorMessage o dili
/// bulamazsa mevcut varsayılana (ilk girdi) düşer, boş string döndürmez. Yani burada dil FİLTRELEMİYORUZ.
/// </summary>
public static class ErrorLanguageResolver
{
    public const string DefaultLanguage = "TR";

    public static string Resolve(string? acceptLanguage, string? legacyLanguageHeader)
    {
        var fromAcceptLanguage = AcceptLanguageParser.PrimaryLanguageTag(acceptLanguage);
        if (!string.IsNullOrWhiteSpace(fromAcceptLanguage))
            return fromAcceptLanguage;

        if (!string.IsNullOrWhiteSpace(legacyLanguageHeader))
            return legacyLanguageHeader.Trim();

        return DefaultLanguage;
    }
}
