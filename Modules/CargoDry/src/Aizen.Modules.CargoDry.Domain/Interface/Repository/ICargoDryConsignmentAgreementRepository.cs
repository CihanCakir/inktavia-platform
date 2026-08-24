using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryConsignmentAgreementRepository
{
    Task<CargoDryConsignmentAgreementEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<CargoDryConsignmentAgreementEntity?> GetByAgreementCodeAsync(
        string agreementCode, CancellationToken ct = default);

    /// <summary>
    /// Returns the single Active agreement for the given provider + product pair that is
    /// valid at <paramref name="nowUtc"/>. Used by allocation logic.
    /// </summary>
    Task<CargoDryConsignmentAgreementEntity?> GetActiveForProviderProductAsync(
        long providerProfileId, string productCode, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>
    /// Returns true if an Active agreement already exists for the provider + product pair.
    /// Pass <paramref name="excludeId"/> when updating an existing agreement to exclude itself from the check.
    /// </summary>
    Task<bool> ExistsActiveForProviderProductAsync(
        long providerProfileId, string productCode, long? excludeId, CancellationToken ct = default);

    /// <summary>
    /// Returns paged results with optional filters.
    /// </summary>
    Task<(List<CargoDryConsignmentAgreementEntity> Items, int Total)> GetPagedAsync(
        long?                       providerProfileId,
        string?                     productCode,
        ConsignmentAgreementStatus? status,
        DateTime?                   dateFrom,
        DateTime?                   dateTo,
        string?                     search,
        int                         skip,
        int                         take,
        CancellationToken           ct = default);

    /// <summary>
    /// Phase 24 — Returns distinct provider profile IDs that have at least one
    /// Active consignment agreement. Used as a candidate source for the CargoDry
    /// opportunity routing priority preview (read-only, decision support only).
    /// </summary>
    Task<IReadOnlyList<long>> GetDistinctActiveProviderProfileIdsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// True when the provider has at least one Active agreement valid at <paramref name="nowUtc"/> — i.e. they
    /// are IN the CargoDry programme, for ANY product.
    ///
    /// PROV-MVP-002: this is the participation predicate the provider-facing write commands and the BFF's
    /// capability lookup both use. It is deliberately product-agnostic; the per-product variants above answer a
    /// different question (may this provider be allocated THIS product), which allocation logic needs and
    /// authorization does not.
    /// </summary>
    Task<int> CountActiveForProviderAsync(
        long providerProfileId, DateTime nowUtc, CancellationToken ct = default);

    Task AddAsync(CargoDryConsignmentAgreementEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
