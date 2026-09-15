using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryStockRequestRepository
{
    Task<CargoDryStockRequestEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<(List<CargoDryStockRequestEntity> Items, int Total)> GetByProviderAsync(
        long providerProfileId, CargoDryStockRequestStatus? status, int skip, int take, CancellationToken ct = default);
    /// <summary>Admin (cross-provider) paged list — optional status + providerProfileId filters, newest first.</summary>
    Task<(List<CargoDryStockRequestEntity> Items, int Total)> GetPagedAsync(
        CargoDryStockRequestStatus? status, long? providerProfileId, int skip, int take, CancellationToken ct = default);
    /// <summary>Most-recent requests for one provider (requester context; cheap unpaged read capped by <paramref name="take"/>).</summary>
    Task<List<CargoDryStockRequestEntity>> GetRecentByProviderAsync(long providerProfileId, int take, CancellationToken ct = default);
    /// <summary>Total requests submitted by a provider (any status) — requester context count.</summary>
    Task<int> CountByProviderAsync(long providerProfileId, CancellationToken ct = default);
    /// <summary>Shipped requests whose auto-receive deadline has elapsed — driven by the auto-receive sweep.</summary>
    Task<IReadOnlyList<long>> GetAutoReceiveDueIdsAsync(DateTimeOffset nowUtc, int maxBatch, CancellationToken ct = default);
    Task<bool> HasPendingForProductAsync(long providerProfileId, string productCode, CancellationToken ct = default);
    Task AddAsync(CargoDryStockRequestEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
