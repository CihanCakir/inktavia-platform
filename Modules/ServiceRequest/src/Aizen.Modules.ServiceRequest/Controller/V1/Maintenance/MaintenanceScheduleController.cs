using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;
using Aizen.Modules.ServiceRequest.Application.Command.Maintenance;
using Aizen.Modules.ServiceRequest.Application.Query.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Maintenance;

/// <summary>
/// S12 — admin management of recurring maintenance schedules (owner self-service arrives with the owner app).
/// Upsert is idempotent by (vessel, category, type); listing is scoped by optional vessel.
/// </summary>
[ApiController]
[Route("api/v1/admin/maintenance-schedules")]
[Tags("Admin - Maintenance Schedules")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Admin maintenance schedule endpoints", "Create/update + list recurring maintenance schedules (S12).")]
public sealed class MaintenanceScheduleController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MaintenanceScheduleController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UpsertMaintenanceScheduleResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpsertMaintenanceScheduleResponse?>> Upsert(
        [FromBody] UpsertMaintenanceScheduleRequest request, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpsertMaintenanceScheduleResponse>(
            new UpsertMaintenanceScheduleCommand(request), ct);
        return SetResponse(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetMaintenanceScheduleListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetMaintenanceScheduleListResponse?>> GetList(
        [FromQuery] long? vesselId = null,
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetMaintenanceScheduleListResponse>(
            new GetMaintenanceScheduleListQuery(vesselId, includeInactive), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// S12 — activate/deactivate a schedule. Deactivating stops N2 reminders and frees the active-unique slot;
    /// reactivating re-arms it (409-style clean business error if another active schedule owns the same key).
    /// Idempotent.
    /// </summary>
    [HttpPut("{id:long}/active")]
    [ProducesResponseType(typeof(SetMaintenanceScheduleActiveResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SetMaintenanceScheduleActiveResponse?>> SetActive(
        long id, [FromBody] SetMaintenanceScheduleActiveRequest request, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SetMaintenanceScheduleActiveResponse>(
            new SetMaintenanceScheduleActiveCommand(id, request.IsActive), ct);
        return SetResponse(result);
    }
}
