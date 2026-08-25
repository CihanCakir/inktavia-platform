using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Query.GetNotificationHistoryDetail;
using Aizen.Modules.Notification.Application.Query.GetNotificationHistoryPaged;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

/// <summary>
/// Faz 28.5 — Admin gönderim geçmişi: gönderilen HER bildirime (tüm kanallar) salt-okunur görünürlük. Yeni bir depo
/// değil, mevcut NotificationEntity üzerinde bir SORGU. Şablon admin ucuyla aynı yetki kuralı (Admin/SuperAdmin).
/// </summary>
[ApiController]
[Route("api/v1/notification/admin/notifications")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminNotificationHistoryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminNotificationHistoryController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>Sayfalı geçmiş: from/to/channel/status/templateCode/recipientUserId filtreleri (hepsi opsiyonel), en yeni önce.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationHistoryListResult>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationHistoryListResult?>> GetHistory(
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
        => SetResponse(await _cqrs.ProcessAsync<NotificationHistoryListResult>(
               new GetNotificationHistoryPagedQuery
               {
                   From = from, To = to, Channel = channel, Status = status,
                   TemplateCode = templateCode, RecipientUserId = recipientUserId, CampaignId = campaignId,
                   Page = page, PageSize = pageSize,
               }, ct));

    /// <summary>Tek bildirimin tam satırı (gövde + metadata + referans/derin-bağlantı dahil). Yoksa boş gövde.</summary>
    [HttpGet("history/{id:long}")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationHistoryDetailDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationHistoryDetailDto?>> GetHistoryDetail(long id, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationHistoryDetailDto>(
               new GetNotificationHistoryDetailQuery { Id = id }, ct));
}
