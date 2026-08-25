using Aizen.Bff.AdminPanel.Application.Notifications.Command;
using Aizen.Bff.AdminPanel.Application.Notifications.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>Faz 28.6 — admin doğrudan/toplu bildirim kampanyaları BFF proxy'si (notifications/ kökü altında, campaigns).</summary>
[ApiController]
[Route("api/v1/admin-panel/notifications/campaigns")]
[Tags("Admin Panel - Notification Campaigns")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class NotificationCampaignsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public NotificationCampaignsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>POST api/v1/admin-panel/notifications/campaigns — kampanya oluştur (Queued + kuyruğa alınır).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(NotificationCampaignMutationResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationCampaignMutationResponse>> Create(
        [FromBody] CreateNotificationCampaignRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationCampaignMutationResponse>(
            new CreateNotificationCampaignBffCommand { Request = body }, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/notifications/campaigns/{id} — kampanya detayı (durum + sayaçlar).</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(NotificationCampaignDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationCampaignDto>> GetById(long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationCampaignDto>(
            new GetNotificationCampaignBffQuery(id), ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/notifications/campaigns — sayfalı kampanya listesi, en yeni önce.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationCampaignListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationCampaignListResult>> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<NotificationCampaignListResult>(
            new GetNotificationCampaignsPagedBffQuery { Page = page, PageSize = pageSize }, ct);
        return SetResponse(result);
    }
}
