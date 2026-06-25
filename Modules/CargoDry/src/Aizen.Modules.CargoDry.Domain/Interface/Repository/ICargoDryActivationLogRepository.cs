using Aizen.Modules.CargoDry.Domain.MongoDocuments;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryActivationLogRepository
{
    Task InsertAsync(CargoDryActivationLogDocument document, CancellationToken ct = default);
    Task<List<CargoDryActivationLogDocument>> GetByKitIdAsync(long kitId, CancellationToken ct = default);
    Task<List<CargoDryActivationLogDocument>> GetByDateRangeAsync(
        string fromDateKey, string toDateKey, string? eventType = null, CancellationToken ct = default);
    Task<Dictionary<string, int>> CountByEventTypeForDateAsync(string dateKey, CancellationToken ct = default);
    Task<List<(string ProductCode, int Count)>> GetActivationsByProductAsync(
        string fromDateKey, string toDateKey, CancellationToken ct = default);
}
