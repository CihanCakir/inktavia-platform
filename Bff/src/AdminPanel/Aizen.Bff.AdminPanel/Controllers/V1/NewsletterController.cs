using System.Text;
using Aizen.Bff.AdminPanel.Application.Notifications.Command;
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
/// P1 — admin newsletter BFF proxy. Same AdminPanelAccess policy and paged/enveloped conventions as
/// <see cref="NotificationHistoryController"/>: subscriber list, dashboard stats, manual suppression, CSV export.
/// </summary>
[ApiController]
[Route("api/v1/admin-panel/newsletter")]
[Tags("Admin Panel - Newsletter")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class NewsletterController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public NewsletterController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>GET api/v1/admin-panel/newsletter/subscribers — paged subscriber list (all filters optional, newest first).</summary>
    [HttpGet("subscribers")]
    [ProducesResponseType(typeof(NewsletterSubscriberListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NewsletterSubscriberListResult>> GetSubscribers(
        [FromQuery] NewsletterSubscriberStatus? status,
        [FromQuery] string? lang,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<NewsletterSubscriberListResult>(new GetNewsletterSubscribersBffQuery
        {
            Status = status, Lang = lang, Search = search, From = from, To = to, Page = page, PageSize = pageSize,
        }, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/newsletter/stats — dashboard counters (per status + new-in-last-30-days).</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(NewsletterStatsDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NewsletterStatsDto>> GetStats(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NewsletterStatsDto>(new GetNewsletterStatsBffQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/newsletter/subscribers/{id}/suppress — manual suppression (audited).</summary>
    [HttpPost("subscribers/{id:long}/suppress")]
    [ProducesResponseType(typeof(NewsletterSubscriberListItemDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NewsletterSubscriberListItemDto>> Suppress(long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NewsletterSubscriberListItemDto>(
            new SuppressNewsletterSubscriberBffCommand { Id = id }, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/newsletter/export — CSV of CONFIRMED subscribers (file download).</summary>
    [HttpGet("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var csv = await _cqrs.ProcessAsync<string>(new ExportNewsletterBffQuery(), ct) ?? string.Empty;
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "newsletter-subscribers.csv");
    }
}
