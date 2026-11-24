using System.Text;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SuppressNewsletterSubscriber;
using Aizen.Modules.Notification.Application.Query.GetNewsletterConfirmedExport;
using Aizen.Modules.Notification.Application.Query.GetNewsletterStats;
using Aizen.Modules.Notification.Application.Query.GetNewsletterSubscribers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

/// <summary>
/// P1 — admin newsletter management. Same authorization rule as the template/history admin ends (Admin/SuperAdmin):
/// paged subscriber list, dashboard stats, manual suppression, and a CONFIRMED-only CSV export.
/// </summary>
[ApiController]
[Route("api/v1/notification/admin/newsletter")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminNewsletterController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminNewsletterController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>Paged subscriber list: status/lang/search/from/to filters (all optional), newest first.</summary>
    [HttpGet("subscribers")]
    [ProducesResponseType(typeof(AizenApiResponse<NewsletterSubscriberListResult>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NewsletterSubscriberListResult?>> GetSubscribers(
        [FromQuery] NewsletterSubscriberStatus? status,
        [FromQuery] string? lang,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<NewsletterSubscriberListResult>(
               new GetNewsletterSubscribersQuery
               {
                   Status = status, Lang = lang, Search = search, From = from, To = to,
                   Page = page, PageSize = pageSize,
               }, ct));

    /// <summary>Dashboard counters: one count per status plus new sign-ups in the last 30 days.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AizenApiResponse<NewsletterStatsDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NewsletterStatsDto?>> GetStats(CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NewsletterStatsDto>(new GetNewsletterStatsQuery(), ct));

    /// <summary>Manually suppress a subscriber (abuse/complaint). Sticky; audited. Returns the updated row.</summary>
    [HttpPost("subscribers/{id:long}/suppress")]
    [ProducesResponseType(typeof(AizenApiResponse<NewsletterSubscriberListItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NewsletterSubscriberListItemDto?>> Suppress(long id, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NewsletterSubscriberListItemDto>(
               new SuppressNewsletterSubscriberCommand { Id = id }, ct));

    /// <summary>CSV export of CONFIRMED subscribers only (not enveloped — a file download).</summary>
    [HttpGet("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var rows = await _cqrs.ProcessAsync<List<NewsletterSubscriberListItemDto>>(
            new GetNewsletterConfirmedExportQuery(), ct) ?? new List<NewsletterSubscriberListItemDto>();

        var sb = new StringBuilder();
        sb.AppendLine("Email,Lang,ConfirmedAt,SourcePage,CreatedAt");
        foreach (var r in rows)
        {
            sb.Append(Csv(r.Email)).Append(',')
              .Append(Csv(r.Lang)).Append(',')
              .Append(Csv(r.ConfirmedAt?.ToString("O"))).Append(',')
              .Append(Csv(r.SourcePage)).Append(',')
              .Append(Csv(r.CreatedAt.ToString("O")))
              .Append('\n');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", "newsletter-subscribers.csv");
    }

    // RFC-4180 minimal escaping: wrap in quotes and double any embedded quote when the value has , " or a newline.
    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
