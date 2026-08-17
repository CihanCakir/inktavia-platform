using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Aizen.Modules.ReferenceData.Repository.Mongo;

/// <summary>
/// M3 — deterministic, Turkish-aware slugify for location names. Mirrors the Content module's <c>SlugService</c> fold
/// (single source of the rule; ReferenceData can't reference Content.Application, so the fold is replicated). Same
/// input → same output every run.
/// </summary>
public static partial class LocationSlugifier
{
    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Turkish-aware fold, then Unicode-normalize away remaining diacritics. The Turkish uppercase dotted 'İ'
        // (U+0130) is handled BEFORE lower-casing — invariant lower-casing turns it into a bare combining dot and
        // would drop the letter; map both 'İ' and 'I' to 'i' up front.
        var folded = value.Trim()
            .Replace('İ', 'i').Replace('I', 'i')
            .ToLowerInvariant()
            .Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u')
            .Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c');

        var normalized = folded.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var ascii = sb.ToString().Normalize(NormalizationForm.FormC);
        ascii = NonSlugCharsRegex().Replace(ascii, "-");   // non [a-z0-9] → '-'
        ascii = MultiDashRegex().Replace(ascii, "-");       // collapse runs of '-'
        return ascii.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharsRegex();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultiDashRegex();
}
