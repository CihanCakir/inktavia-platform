using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.Interface.Service;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Produces URL-safe, unique content slugs (deferred from C2). Uniqueness is checked against the
/// repository's soft-delete-aware <see cref="IContentItemRepository.SlugExistsAsync"/>, so a
/// soft-deleted item releases its slug.
/// </summary>
public sealed partial class SlugService : ISlugService
{
    private readonly IContentItemRepository _items;

    public SlugService(IContentItemRepository items) => _items = items;

    public string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Turkish-aware fold, then Unicode-normalize away remaining diacritics.
        var folded = value.Trim().ToLowerInvariant()
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

    public async Task<string> GenerateUniqueSlugAsync(string desiredSlugOrTitle, CancellationToken ct = default)
    {
        var baseSlug = Slugify(desiredSlugOrTitle);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "content";

        var candidate = baseSlug;
        var suffix = 2;
        while (await _items.SlugExistsAsync(candidate, excludeId: null, ct))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharsRegex();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultiDashRegex();
}
