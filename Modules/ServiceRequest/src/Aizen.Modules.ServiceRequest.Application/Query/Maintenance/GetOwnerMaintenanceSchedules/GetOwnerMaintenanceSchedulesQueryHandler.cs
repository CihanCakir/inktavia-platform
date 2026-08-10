using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.Maintenance;

/// <summary>
/// BE-MO8 — lists the caller-owner's own maintenance schedules via the additive owner-scoped repo query
/// (<c>ListByOwnerAsync</c>). Owner-scoped: only schedules whose <c>OwnerUserId</c> equals the caller cross. Reuses
/// the S12 list response + DTO mapping unchanged.
/// </summary>
public sealed class GetOwnerMaintenanceSchedulesQueryHandler
    : AizenQueryHandler<GetOwnerMaintenanceSchedulesQuery, GetMaintenanceScheduleListResponse>
{
    private readonly IMaintenanceScheduleRepository _repository;

    public GetOwnerMaintenanceSchedulesQueryHandler(IMaintenanceScheduleRepository repository)
        => _repository = repository;

    public override async Task<GetMaintenanceScheduleListResponse?> Handle(
        GetOwnerMaintenanceSchedulesQuery request, CancellationToken cancellationToken)
    {
        // No owner identity → no schedules (never fabricate).
        if (request.OwnerUserId <= 0)
            return new GetMaintenanceScheduleListResponse(new List<MaintenanceScheduleDto>());

        var schedules = await _repository.ListByOwnerAsync(
            request.OwnerUserId, request.VesselId, request.IncludeInactive, cancellationToken);

        return new GetMaintenanceScheduleListResponse(schedules.Select(s => s.ToDto()).ToList());
    }
}
