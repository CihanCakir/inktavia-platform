using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

/// <summary>
/// Repository for the renewal preparation workflow entity.
/// Phase 11 (July 2026).
/// </summary>
public interface ICargoDryRenewalPreparationRepository
{
    /// <summary>Returns a single preparation by its surrogate PK.</summary>
    Task<CargoDryRenewalPreparationEntity?> GetByIdAsync(
        long id, CancellationToken ct = default);

    /// <summary>Returns a preparation by its human-readable renewal code.</summary>
    Task<CargoDryRenewalPreparationEntity?> GetByRenewalCodeAsync(
        string renewalCode, CancellationToken ct = default);

    /// <summary>
    /// Returns the open (non-terminal) preparation for a given kit, or null.
    /// Used to enforce the one-open-preparation-per-kit rule.
    /// </summary>
    Task<CargoDryRenewalPreparationEntity?> GetOpenForKitAsync(
        long kitId, CancellationToken ct = default);

    /// <summary>
    /// Returns paged preparations with optional filters.
    /// </summary>
    Task<(List<CargoDryRenewalPreparationEntity> Items, int Total)> GetPagedAsync(
        long?                              kitId,
        string?                            kitCode,
        string?                            productCode,
        long?                              ownerUserId,
        long?                              vesselId,
        CargoDryRenewalPreparationStatus?  status,
        CargoDryRenewalNotificationStatus? notificationStatus,
        DateTimeOffset?                    preparedFrom,
        DateTimeOffset?                    preparedTo,
        int                                skip,
        int                                take,
        CancellationToken                  ct = default);

    /// <summary>
    /// Returns all kits whose ExpiresAtUtc falls within the candidate window
    /// that do NOT have an open preparation.
    /// </summary>
    Task<List<long>> GetKitIdsWithOpenPreparationAsync(
        CancellationToken ct = default);

    /// <summary>Stages a new preparation. Caller calls SaveChangesAsync.</summary>
    Task AddAsync(CargoDryRenewalPreparationEntity entity, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
