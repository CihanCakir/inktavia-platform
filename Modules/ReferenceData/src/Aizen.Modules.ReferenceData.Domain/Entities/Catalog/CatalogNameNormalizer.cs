using System.Text.RegularExpressions;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Catalog;

/// <summary>
/// Name normalization for the brand/model catalog. <see cref="Clean"/> is the stored form (trim + collapse internal
/// whitespace); <see cref="Key"/> is the case-insensitive dedupe key (Clean + upper-invariant). "Beneteau",
/// "  beneteau ", "BENETEAU" all share one <see cref="Key"/> — the "not in list" submission returns the existing row.
/// </summary>
public static class CatalogNameNormalizer
{
    public static string Clean(string? name)
        => Regex.Replace((name ?? string.Empty).Trim(), @"\s+", " ");

    public static string Key(string? name)
        => Clean(name).ToUpperInvariant();

    /// <summary>Opaque uppercase code slug from a name (non-alphanumeric → "_"). Callers ensure uniqueness.</summary>
    public static string Slug(string? name)
    {
        var s = Regex.Replace(Key(name), "[^A-Z0-9]+", "_").Trim('_');
        return string.IsNullOrEmpty(s) ? "ITEM" : s;
    }
}
