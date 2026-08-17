using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Messaging.Application.Query.GetChannelUsageReport;
using Aizen.Modules.Messaging.Application.Query.GetProviderResponseTimeReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Messaging.Controller.V1.Reporting;

[ApiController]
[Route("api/v1/reporting/messaging")]
[Tags("Messaging - Reporting")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Messaging reporting controller",
    "Admin-only reporting endpoints: provider response time and channel usage analytics.")]
public sealed class MessagingReportingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MessagingReportingController(IHttpContextAccessor accessor, IAizenCQRSProcessor cqrs)
        : base(accessor) => _cqrs = cqrs;

    /// <summary>
    /// Provider first-response time report.
    /// GET /api/v1/reporting/messaging/provider-response-time
    /// </summary>
    [HttpGet("provider-response-time")]
    public async Task<AizenApiResponse<GetProviderResponseTimeReportResponse?>> ProviderResponseTime(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderResponseTimeReportResponse>(
            new GetProviderResponseTimeReportQuery { From = from, To = to, Take = take }, ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Channel usage analytics (last 30 days by default).
    /// GET /api/v1/reporting/messaging/channel-usage
    /// </summary>
    [HttpGet("channel-usage")]
    public async Task<AizenApiResponse<GetChannelUsageReportResponse?>> ChannelUsage(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetChannelUsageReportResponse>(
            new GetChannelUsageReportQuery
            {
                From = from ?? DateTimeOffset.UtcNow.AddDays(-30),
                To   = to   ?? DateTimeOffset.UtcNow
            }, ct);
        return SetResponse(result);
    }
}
