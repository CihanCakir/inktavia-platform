namespace Aizen.Bff.Marine.Web.Application.Common.Text;

/// <summary>
/// The locales the public website publishes, and the slug transform every locale-aware projection uses.
///
/// ── Why this lives in the BFF ───────────────────────────────────────────────
/// D-02 in the frontend's binding route contract: entity slugs are BFF-owned and ASCII-folded, and the frontend
/// folds nothing. One implementation here means every consumer derives the SAME slug for the same entity — which is
/// the whole point, because a slug is a domain identifier and a change to one is a domain event that needs a
/// redirect, not a rendering detail that each client may compute differently.
///
/// ── Turkish folding is not culture-invariant lower-casing ───────────────────
/// `ToLowerInvariant()` leaves 'ı', 'ş', 'ğ', 'ç', 'ö', 'ü' untouched and maps 'İ' to a dotted 'i̇' (i + combining
/// dot) — both produce percent-encoded, unreadable URLs. The fold below is an explicit character map, applied BEFORE
/// case folding, so "Motor Bakımı" becomes "motor-bakimi" and never "motor-bak%C4%B1m%C4%B1". The map is exactly the
/// one the route contract specifies: ı→i, İ→i, ş→s, ğ→g, ç→c, ö→o, ü→u.
/// </summary>
public static class WebLocale
{
    /// <summary>Default locale when a caller sends no <c>lang</c>. English, so an existing caller's response is unchanged.</summary>
    public const string Default = "en";

    public const string Turkish = "tr";
    public const string English = "en";

    /// <summary>The locales a projection returns slugs for. Adding one is a data question, not a code change here.</summary>
    public static readonly string[] Supported = { Turkish, English };

    /// <summary>
    /// Normalises a caller-supplied <c>lang</c> to a supported locale. Anything unrecognised — including null, a
    /// region tag we do not publish, or junk — falls back to <see cref="Default"/> rather than erroring: a marketing
    /// page must render, and answering an odd Accept-Language with a 400 would take the page down for no benefit.
    /// A region tag for a supported language ("tr-TR") resolves to its language.
    /// </summary>
    public static string Normalize(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
            return Default;

        var value = lang.Trim();
        var dash = value.IndexOf('-');
        if (dash > 0)
            value = value[..dash];

        foreach (var supported in Supported)
        {
            if (string.Equals(value, supported, StringComparison.OrdinalIgnoreCase))
                return supported;
        }

        return Default;
    }

    /// <summary>
    /// A string comparer collating in the given locale, for ordering display names. Derived from the requested
    /// locale rather than from the process's ambient culture, so the same request sorts identically on every host —
    /// which matters because the response body feeds a weak ETag.
    /// </summary>
    public static StringComparer NameComparer(string locale) => StringComparer.Create(
        System.Globalization.CultureInfo.GetCultureInfo(locale == Turkish ? "tr-TR" : "en-US"),
        ignoreCase: true);

    /// <summary>
    /// Deterministic text → URL segment: Turkish characters folded to ASCII, lower-cased, every run of
    /// non-alphanumerics collapsed to a single dash, dashes trimmed from both ends. A pure transform of a value that
    /// already exists — it never invents data. Returns an empty string for input that folds to nothing, and callers
    /// must treat that as "no slug for this locale" rather than publishing an empty segment.
    /// </summary>
    public static string ToSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new System.Text.StringBuilder(value.Length);
        var lastWasDash = false;

        foreach (var raw in value.Trim())
        {
            var ch = Fold(raw);

            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(ch);
                lastWasDash = false;
            }
            else if (ch is >= 'A' and <= 'Z')
            {
                builder.Append((char)(ch + 32));
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static char Fold(char ch) => ch switch
    {
        'ı' or 'İ' => 'i',
        'ş' or 'Ş' => 's',
        'ğ' or 'Ğ' => 'g',
        'ç' or 'Ç' => 'c',
        'ö' or 'Ö' => 'o',
        'ü' or 'Ü' => 'u',
        'â' or 'Â' => 'a',
        'î' or 'Î' => 'i',
        'û' or 'Û' => 'u',
        _ => ch,
    };
}
