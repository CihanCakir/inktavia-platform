namespace Aizen.Core.Common.Abstraction.Configuration;

/// <summary>
/// FAZ14 (#30) — chart values / appsettings içindeki doldurulmamış yapılandırma yer-tutucularının TEK kaynağı.
///
/// <c>__FROM_ENV__</c> ve <c>__FROM_SECRET__</c> BOŞ değil, dolu birer string'tir; bu yüzden
/// <c>string.IsNullOrWhiteSpace</c> gibi kontroller onları "dolu" sanar. Auth/remote-call kararları bu yüzden
/// yanlış dala girip <c>o.Authority = "__FROM_ENV__"</c> ya da <c>BaseAddress = "__FROM_ENV__"</c> gibi çöp
/// değerlerle çalışır (borç #50/#53/#54/#57/#58/#61/#62). Bir config değerini "gerçekten ayarlı mı?" diye
/// yorumlayan her yerde <see cref="NullIfUnset"/> kullanın; böylece yer-tutucu = ayarlanmamış sayılır.
/// </summary>
public static class AizenConfigPlaceholders
{
    public const string FromEnv = "__FROM_ENV__";
    public const string FromSecret = "__FROM_SECRET__";

    /// <summary>Değer doldurulmamış bir yer-tutucuya eşit mi? (boşluk kontrolü yapmaz — yalnız yer-tutucu.)</summary>
    public static bool IsPlaceholder(string? value) => value is FromEnv or FromSecret;

    /// <summary>
    /// Değer boş VEYA doldurulmamış bir yer-tutucuysa <c>null</c> döndürür; aksi hâlde değerin kendisini.
    /// "Config gerçekten ayarlı mı?" kararlarında ham okuma yerine bunu kullanın:
    /// <c>NullIfUnset(cfg["Keycloak:Authority"]) is { } authority</c>.
    /// </summary>
    public static string? NullIfUnset(string? value)
        => string.IsNullOrWhiteSpace(value) || IsPlaceholder(value) ? null : value;
}
