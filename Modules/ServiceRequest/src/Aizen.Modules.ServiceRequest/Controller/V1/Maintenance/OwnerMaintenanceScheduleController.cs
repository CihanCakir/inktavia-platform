using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;
using Aizen.Modules.ServiceRequest.Application.Command.Maintenance;
using Aizen.Modules.ServiceRequest.Application.Query.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Maintenance;

/// <summary>
/// BE-MO8 — owner self-service of recurring maintenance schedules for the caller's OWN vessels. Separate from the
/// admin controller (which stays <c>[Authorize(Roles="Admin")]</c> and untouched): this is participant/service-token
/// authenticated (callable by the mobile BFF via the BffAssertion path), and the owner identity is taken from the
/// token — <c>OwnerUserId</c> is always the caller, never the body. The vessel-ownership gate (the caller may only
/// schedule their own vessels) is enforced by the mobile BFF before it proxies here. Reuses the S12 engine (upsert /
/// list / set-active) unchanged; adds only owner-scoping.
/// </summary>
[ApiController]
[Route("api/v1/service-requests/maintenance-schedules")]
[Tags("ServiceRequest - Owner Maintenance Schedules")]
[Authorize]
[DocumentationInfo("Owner maintenance schedule endpoints", "Owner self-service: list/upsert/set-active for the caller's own vessels (MO8).")]
public sealed class OwnerMaintenanceScheduleController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IAizenInfoAccessor _info;

    public OwnerMaintenanceScheduleController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs, IAizenInfoAccessor info)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _info = info;
    }

    // Prefer the BFF-asserted identity (UserInfo.UserId); fall back to the NameIdentifier claim. Mirrors
    // ServiceRequestController.CurrentUserId so a BFF caller whose NameIdentifier is the service-account subject works.
    private long CurrentUserId
    {
        get
        {
            var asserted = _info.UserInfoAccessor?.UserInfo?.UserId ?? 0;
            if (asserted > 0)
                return asserted;

            var raw = ContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(raw, out var id) ? id : 0;
        }
    }

    /// <summary>The caller-owner's own schedules (includeInactive so deactivated ones stay reactivatable).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetMaintenanceScheduleListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetMaintenanceScheduleListResponse?>> GetMyList(
        [FromQuery] long? vesselId = null,
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetMaintenanceScheduleListResponse>(
            new GetOwnerMaintenanceSchedulesQuery(CurrentUserId, vesselId, includeInactive), ct);
        return SetResponse(result);
    }

    /// <summary>Create/update a schedule for one of the caller's own vessels. Idempotent by (vessel, category, type).
    /// <c>OwnerUserId</c> is stamped from the token — any body value is ignored. Vessel-ownership is BFF-gated.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpsertMaintenanceScheduleResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpsertMaintenanceScheduleResponse?>> Upsert(
        [FromBody] UpsertMaintenanceScheduleRequest request, CancellationToken ct = default)
    {
        // Owner identity is authoritative — never trust the body's OwnerUserId.
        request.OwnerUserId = CurrentUserId;

        var result = await _cqrs.ProcessAsync<UpsertMaintenanceScheduleResponse>(
            new UpsertMaintenanceScheduleCommand(request), ct);
        return SetResponse(result);
    }

    /// <summary>Activate/deactivate one of the caller's own schedules (owner-gated + the reactivate-conflict guard).</summary>
    [HttpPut("{id:long}/active")]
    [ProducesResponseType(typeof(SetMaintenanceScheduleActiveResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SetMaintenanceScheduleActiveResponse?>> SetActive(
        long id, [FromBody] SetMaintenanceScheduleActiveRequest request, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SetMaintenanceScheduleActiveResponse>(
            new SetOwnerMaintenanceScheduleActiveCommand(CurrentUserId, id, request.IsActive), ct);
        return SetResponse(result);
    }
}
