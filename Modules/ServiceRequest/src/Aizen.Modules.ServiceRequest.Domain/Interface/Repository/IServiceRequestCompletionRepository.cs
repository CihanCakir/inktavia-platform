using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request completion repository interface", "Data access contract for ServiceRequestCompletion entities.")]
public interface IServiceRequestCompletionRepository
{
    Task<ServiceRequestCompletionEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    Task AddAsync(ServiceRequestCompletionEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestCompletionEntity entity);

    /// <summary>
    /// N3-C — still-pending (Submitted) completions whose auto-approval deadline is at or before <paramref name="cutoffUtc"/>
    /// (pass <c>now + reminderLead</c> to fetch both approaching-reminder and past-deadline candidates in one query).
    /// Only rows with a set <c>AutoApproveAt</c>. Ordered by deadline. Read for the auto-approval job.
    /// </summary>
    Task<IReadOnlyList<ServiceRequestCompletionEntity>> GetPendingAutoApproveCandidatesAsync(
        DateTime cutoffUtc, int maxBatch, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
