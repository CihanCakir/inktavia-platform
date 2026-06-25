using Aizen.Modules.CargoDry.Domain.MongoDocuments;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDrySnapshotRepository
{
    Task UpsertAsync(CargoDryKitUsageSnapshotDocument document, CancellationToken ct = default);
    Task<List<CargoDryKitUsageSnapshotDocument>> GetRecentAsync(int days = 30, CancellationToken ct = default);
    Task<CargoDryKitUsageSnapshotDocument?> GetByDateKeyAsync(string dateKey, CancellationToken ct = default);
}
