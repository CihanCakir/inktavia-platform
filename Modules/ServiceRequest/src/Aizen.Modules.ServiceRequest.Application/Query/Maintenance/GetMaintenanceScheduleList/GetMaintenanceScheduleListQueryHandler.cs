using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.Maintenance;

[DocumentationInfo("Get maintenance schedule list handler", "Returns active maintenance schedules for admin view (S12).")]
public sealed class GetMaintenanceScheduleListQueryHandler
    : AizenQueryHandler<GetMaintenanceScheduleListQuery, GetMaintenanceScheduleListResponse>
{
    private readonly IMaintenanceScheduleRepository _repository;

    public GetMaintenanceScheduleListQueryHandler(IMaintenanceScheduleRepository repository)
        => _repository = repository;

    public override async Task<GetMaintenanceScheduleListResponse?> Handle(
        GetMaintenanceScheduleListQuery request, CancellationToken cancellationToken)
    {
        var schedules = await _repository.ListAsync(request.VesselId, request.IncludeInactive, cancellationToken);
        return new GetMaintenanceScheduleListResponse(schedules.Select(s => s.ToDto()).ToList());
    }
}
