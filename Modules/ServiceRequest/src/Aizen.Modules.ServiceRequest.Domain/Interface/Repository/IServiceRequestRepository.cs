using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request repository interface", "Data access contract for the ServiceRequest aggregate root.")]
public interface IServiceRequestRepository
{
    Task<ServiceRequestEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ServiceRequestEntity?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default);
    Task<ServiceRequestEntity?> GetByCodeAsync(string requestCode, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestEntity>> GetByOwnerUserIdAsync(long ownerUserId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByOwnerUserIdAsync(long ownerUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default);
    Task AddAsync(ServiceRequestEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestEntity entity);
}
