using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

/// <summary>
/// CargoDry supply flow (additive): owner → preferred CargoDry provider store. Set-once on the owner's first
/// completed CARGODRY_SUPPLY SR; read by provider discovery to flag/sort the owner's later requests.
/// </summary>
public interface ICargoDryOwnerPreferredProviderRepository
{
    Task<CargoDryOwnerPreferredProviderEntity?> GetByOwnerAsync(long ownerUserId, CancellationToken ct = default);

    /// <summary>
    /// Owner user ids whose preferred CargoDry provider is <paramref name="providerProfileId"/>. Used by provider
    /// discovery to compute <c>isPreferred</c>/sort for a page of CARGODRY_SUPPLY requests in one round-trip.
    /// </summary>
    Task<HashSet<long>> GetOwnerIdsPreferringProviderAsync(long providerProfileId, CancellationToken ct = default);

    Task AddAsync(CargoDryOwnerPreferredProviderEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
