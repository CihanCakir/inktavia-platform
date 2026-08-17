namespace Aizen.Modules.Identity.Domain.Interface.Repository;

/// <summary>One eligible provider returned by the area read-model.</summary>
public readonly record struct ProviderAreaRow(long ProfileId, long UserId);

public interface IProviderServiceCategoryRepository
{
    /// <summary>Stage a full replace of a provider's category rows (delete existing + add normalized new). Caller commits.</summary>
    Task ReplaceForProfileAsync(long profileId, long userId, IEnumerable<string> serviceCategoryCodes, CancellationToken ct = default);

    /// <summary>Whether a provider already has any category rows (used by the idempotent backfill).</summary>
    Task<bool> HasAnyForProfileAsync(long profileId, CancellationToken ct = default);

    /// <summary>
    /// I2 read-model: active + approved organizer providers whose City matches (and, if a category code is given,
    /// who serve that category). Capped by <paramref name="take"/>. MVP city-level.
    /// </summary>
    Task<List<ProviderAreaRow>> GetProvidersForAreaAsync(
        string cityCode, string? serviceCategoryCode, int take, CancellationToken ct = default);

    /// <summary>
    /// M2 — counts eligible providers for a (city, optional category), fetching AT MOST <paramref name="cap"/> rows
    /// (server-side LIMIT) so a coarse verdict never materializes a full list or a real total. Same eligibility
    /// filter as <see cref="GetProvidersForAreaAsync"/> (approved + active organizer profiles in the city; optional
    /// canonical category). Returns a value in <c>[0, cap]</c> — the caller buckets it and discards the number.
    /// </summary>
    Task<int> CountForAreaAsync(
        string cityCode, string? serviceCategoryCode, int cap, CancellationToken ct = default);
}
