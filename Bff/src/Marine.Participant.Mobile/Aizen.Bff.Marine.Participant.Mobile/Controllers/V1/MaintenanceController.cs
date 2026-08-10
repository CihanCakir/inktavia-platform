using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Maintenance;
using Aizen.Bff.Marine.Participant.Mobile.Application.Maintenance;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>
/// BE_MO8 — owner maintenance self-service: view / create-edit / activate-deactivate recurring maintenance schedules
/// for the caller's OWN vessels. Identity is resolved from the token and asserted to the SR owner endpoints (OwnerUserId
/// is stamped module-side, never the body); the vessel-ownership gate (own vessels only) is enforced on upsert. Cost-free.
/// </summary>
[ApiController]
[Route("api/v1/mobile/maintenance-schedules")]
[Tags("Mobile - Maintenance Schedules")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class MaintenanceController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MaintenanceController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The caller-owner's own schedules (optionally one vessel; includeInactive so deactivated stay reactivatable).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MobileMaintenanceScheduleListDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileMaintenanceScheduleListDto>> GetMy(
        [FromQuery] long? vesselId = null, [FromQuery] bool includeInactive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileMaintenanceSchedulesQuery(vesselId, includeInactive), ct);
        return SetResponse(result);
    }

    /// <summary>Create/edit a schedule for one of the caller's own vessels (idempotent by vessel+category+type).
    /// Vessel-ownership gated; a duplicate-active / reactivate conflict surfaces as a clean business error.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MobileMaintenanceUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileMaintenanceUpsertResultDto>> Upsert(
        [FromBody] MobileUpsertMaintenanceScheduleRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpsertMobileMaintenanceScheduleCommand(request ?? new MobileUpsertMaintenanceScheduleRequest()), ct);
        return SetResponse(result);
    }

    /// <summary>Activate/deactivate one of the caller's own schedules (deactivate stops the N2 reminder).</summary>
    [HttpPut("{scheduleId:long}/active")]
    [ProducesResponseType(typeof(MobileMaintenanceSetActiveResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileMaintenanceSetActiveResultDto>> SetActive(
        [FromRoute] long scheduleId, [FromBody] MobileSetMaintenanceScheduleActiveRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new SetMobileMaintenanceScheduleActiveCommand(scheduleId, request?.IsActive ?? false), ct);
        return SetResponse(result);
    }
}
