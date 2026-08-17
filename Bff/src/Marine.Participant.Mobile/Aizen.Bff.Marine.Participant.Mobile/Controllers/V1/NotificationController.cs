using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Bff.Marine.Participant.Mobile.Application.Notification;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>
/// BE_MO9c — the owner notification surface: inbox, mark-read/mark-all, FCM device-token registration, and the
/// per-category preference matrix. All passthroughs over the existing Notification module endpoints, which resolve the
/// recipient from the token (BffAssertion → ProviderProfileId) — the recipient is never in the body. Cost-free; the
/// WebPush endpoints are not exposed to mobile.
/// </summary>
[ApiController]
[Route("api/v1/mobile/notifications")]
[Tags("Mobile - Notifications")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class NotificationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public NotificationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The caller's notification inbox (paged) + the unread badge count. Cost-free.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MobileNotificationListDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileNotificationListDto>> GetInbox(
        [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileNotificationsQuery(skip, take), ct);
        return SetResponse(result);
    }

    /// <summary>Mark one of the caller's notifications read.</summary>
    [HttpPatch("{id:long}/read")]
    [ProducesResponseType(typeof(MobileMarkReadResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileMarkReadResultDto>> MarkRead([FromRoute] long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new MarkMobileNotificationReadCommand(id), ct);
        return SetResponse(result);
    }

    /// <summary>Mark all of the caller's notifications read.</summary>
    [HttpPost("mark-all-read")]
    [ProducesResponseType(typeof(MobileMarkAllReadResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileMarkAllReadResultDto>> MarkAllRead(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new MarkAllMobileNotificationsReadCommand(), ct);
        return SetResponse(result);
    }

    /// <summary>Register the caller's FCM push token (Platform forced to Fcm; feeds MO9a push).</summary>
    [HttpPost("device-token")]
    [ProducesResponseType(typeof(MobileDeviceTokenResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileDeviceTokenResultDto>> RegisterDeviceToken(
        [FromBody] MobileRegisterDeviceTokenRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new RegisterMobileDeviceTokenCommand(request?.DeviceToken), ct);
        return SetResponse(result);
    }

    /// <summary>The caller's per-category × channel preference matrix.</summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(MobileNotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileNotificationPreferencesDto>> GetPreferences(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileNotificationPreferencesQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>Toggle one category×channel preference (only Push/Email; the module rejects locked cells).</summary>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(MobileNotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileNotificationPreferencesDto>> UpdatePreference(
        [FromBody] MobileUpdatePreferenceRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateMobileNotificationPreferenceCommand(request ?? new MobileUpdatePreferenceRequest()), ct);
        return SetResponse(result);
    }
}
