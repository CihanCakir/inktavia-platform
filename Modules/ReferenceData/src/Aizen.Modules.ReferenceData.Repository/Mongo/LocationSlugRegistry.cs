namespace Aizen.Modules.ReferenceData.Repository.Mongo;

/// <summary>
/// M3 — the pure slug-uniqueness engine used by the backfill. Pre-seeded with the reserved slugs (W3.4) so any base
/// landing on one is suffixed away. <see cref="Reserve"/> registers an already-assigned slug (idempotency — existing
/// slugs are never reissued); <see cref="Assign"/> returns the first globally-unique candidate for a base
/// (<c>kadikoy</c> → <c>kadikoy-2</c> → …). Deterministic: given the same reserve/assign call order it yields the same
/// slugs, so a re-run over a fully-slugged dataset assigns nothing.
/// </summary>
public sealed class LocationSlugRegistry
{
    private readonly HashSet<string> _used;

    public LocationSlugRegistry()
        => _used = new HashSet<string>(LocationReservedSlugs.Values, StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers an existing slug as taken (does not reissue it).</summary>
    public void Reserve(string slug)
    {
        if (!string.IsNullOrWhiteSpace(slug))
            _used.Add(slug);
    }

    /// <summary>Returns the first globally-unique candidate for <paramref name="baseSlug"/> and marks it used.</summary>
    public string Assign(string baseSlug)
    {
        var candidate = baseSlug;
        var n = 2;
        while (!_used.Add(candidate))
            candidate = $"{baseSlug}-{n++}";
        return candidate;
    }
}
