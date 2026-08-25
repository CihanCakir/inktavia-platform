using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.CreateNotificationCampaign;
using Aizen.Modules.Notification.Application.Query.GetNotificationCampaign;
using Aizen.Modules.Notification.Application.Query.GetNotificationCampaignsPaged;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

/// <summary>
/// Faz 28.6 — admin doğrudan/toplu bildirim kampanyaları. Oluşturma kampanyayı Queued kaydeder ve
/// NotificationCampaignQueuedMessage yayınlar; CampaignDispatchConsumer mevcut kanal dispatcher'ları üzerinden dağıtır.
/// </summary>
[ApiController]
[Route("api/v1/notification/admin/notifications/campaigns")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminNotificationCampaignsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminNotificationCampaignsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>Kampanya oluştur → Queued + kuyruğa alınır. Doğrulama: şablon yayınlanmış içerik / custom tüm-locale / Sms reddi.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationCampaignMutationResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationCampaignMutationResponse?>> Create(
        [FromBody] CreateNotificationCampaignRequest body, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationCampaignMutationResponse>(
               new CreateNotificationCampaignCommand
               {
                   Audience             = body.Audience,
                   TargetMode           = body.TargetMode,
                   SelectedRecipientIds = body.SelectedRecipientIds,
                   TemplateCode         = body.TemplateCode,
                   CustomContent        = body.CustomContent,
                   Channels             = body.Channels,
                   ScheduledAt          = body.ScheduledAt,
               }, ct));

    /// <summary>Kampanya detayı: durum + sayaçlar.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationCampaignDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationCampaignDto?>> GetById(long id, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationCampaignDto>(
               new GetNotificationCampaignQuery { Id = id }, ct));

    /// <summary>Sayfalı kampanya listesi, en yeni önce.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationCampaignListResult>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationCampaignListResult?>> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<NotificationCampaignListResult>(
               new GetNotificationCampaignsPagedQuery { Page = page, PageSize = pageSize }, ct));
}
