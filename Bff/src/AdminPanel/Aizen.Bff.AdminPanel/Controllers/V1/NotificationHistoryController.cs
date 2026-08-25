using Aizen.Bff.AdminPanel.Application.Notifications.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>
/// Faz 28.5 — Admin gönderim geçmişi BFF proxy'si. notifications/ kökünü paylaşır (mevcut notifications/unread-count'a
/// dokunmaz); admin şablon uçlarıyla aynı AdminPanelAccess politikası. Şablon paged BFF handler şekillerini izler.
/// </summary>
[ApiController]
[Route("api/v1/admin-panel/notifications")]
[Tags("Admin Panel - Notification History")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class NotificationHistoryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public NotificationHistoryController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>GET api/v1/admin-panel/notifications/history — sayfalı gönderim geçmişi (tüm filtreler opsiyonel, en yeni önce).</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(NotificationHistoryListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationHistoryListResult>> GetHistory(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] NotificationChannel? channel,
        [FromQuery] NotificationStatus? status,
        [FromQuery] string? templateCode,
        [FromQuery] long? recipientUserId,
        [FromQuery] long? campaignId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<NotificationHistoryListResult>(new GetNotificationHistoryBffQuery
        {
            From = from, To = to, Channel = channel, Status = status,
            TemplateCode = templateCode, RecipientUserId = recipientUserId, CampaignId = campaignId,
            Page = page, PageSize = pageSize,
        }, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/notifications/history/{id} — tek bildirimin tam satırı (gövde + metadata dahil).</summary>
    [HttpGet("history/{id:long}")]
    [ProducesResponseType(typeof(NotificationHistoryDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationHistoryDetailDto>> GetHistoryDetail(long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationHistoryDetailDto>(
            new GetNotificationHistoryDetailBffQuery(id), ct);
        return SetResponse(result);
    }
}
