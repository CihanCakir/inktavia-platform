using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/messaging")]
[Tags("Admin Panel - Messaging")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class MessagingController : AizenWebApiController
{
    private readonly IMessagingRemoteCall _messaging;

    public MessagingController(IHttpContextAccessor httpContextAccessor, IMessagingRemoteCall messaging)
        : base(httpContextAccessor) => _messaging = messaging;

    // ─── Conversations ────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/messaging/conversations</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(GetConversationListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationListResponse>> GetConversations(
        [FromQuery] string? status      = null,
        [FromQuery] string? contextType = null,
        [FromQuery] int     skip        = 0,
        [FromQuery] int     take        = 20,
        CancellationToken ct            = default)
    {
        // The remote call already returns the module's AizenApiResponse envelope; pass it straight through
        // (do NOT SetResponse again — that would double-wrap). This preserves the module's header + body.
        var result = await _messaging.GetConversationsAsync(status, contextType, skip, take, ct);
        return result;
    }

    /// <summary>GET api/v1/admin-panel/messaging/conversations/{id}</summary>
    [HttpGet("conversations/{id:long}")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse>> GetConversation(
        long id,
        CancellationToken ct = default)
    {
        var result = await _messaging.GetConversationAsync(id, ct);
        return result;
    }

    // ─── Messages ─────────────────────────────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/messaging/conversations/{id}/messages</summary>
    [HttpPost("conversations/{id:long}/messages")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SendMessageResponse>> SendMessage(
        long id,
        [FromBody] SendMessageRequest body,
        CancellationToken ct = default)
    {
        var result = await _messaging.SendMessageAsync(id, body, ct);
        return result;
    }

    /// <summary>PATCH api/v1/admin-panel/messaging/conversations/{id}/mark-read</summary>
    [HttpPatch("conversations/{id:long}/mark-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct = default)
    {
        await _messaging.MarkReadAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST api/v1/admin-panel/messaging/conversations/{id}/attachment-upload-url</summary>
    [HttpPost("conversations/{id:long}/attachment-upload-url")]
    [ProducesResponseType(typeof(RequestAttachmentUploadUrlResponseBff), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RequestAttachmentUploadUrlResponseBff>> GetAttachmentUploadUrl(
        long id,
        [FromBody] AttachmentUploadUrlRequest body,
        CancellationToken ct = default)
    {
        // The remote call already returns the module's AizenApiResponse envelope; pass it straight
        // through (do NOT SetResponse again — that would double-wrap and 500 the FE).
        var result = await _messaging.GetAttachmentUploadUrlAsync(id, body, ct);
        return result;
    }

    // ─── Moderation ───────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/messaging/moderation/queue</summary>
    [HttpGet("moderation/queue")]
    [ProducesResponseType(typeof(GetModerationQueueResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetModerationQueueResponse>> GetModerationQueue(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _messaging.GetModerationQueueAsync(skip, take, ct);
        return result;
    }

    /// <summary>PATCH api/v1/admin-panel/messaging/moderation/messages/{id}</summary>
    [HttpPatch("moderation/messages/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ModerateMessage(
        long id,
        [FromBody] ModerateMessageRequest body,
        CancellationToken ct = default)
    {
        await _messaging.ModerateMessageAsync(id, body, ct);
        return NoContent();
    }

    /// <summary>PATCH api/v1/admin-panel/messaging/moderation/conversations/{id}/flag</summary>
    [HttpPatch("moderation/conversations/{id:long}/flag")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FlagConversation(
        long id,
        [FromBody] FlagConversationRequest body,
        CancellationToken ct = default)
    {
        await _messaging.FlagConversationAsync(id, body, ct);
        return NoContent();
    }

    // ─── Reporting ────────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/v1/admin-panel/messaging/reports
    /// Fans out provider-response-time + channel-usage, unwraps each module envelope, and merges
    /// them into one typed <see cref="MessagingReportsBffResponse"/> (no anonymous object / raw envelope leakage).
    /// </summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(MessagingReportsBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MessagingReportsBffResponse>> GetReports(
        [FromQuery] string? from = null,
        [FromQuery] string? to   = null,
        CancellationToken ct     = default)
    {
        var providerTask = _messaging.GetProviderResponseTimeAsync(from, to, ct);
        var channelTask  = _messaging.GetChannelUsageAsync(from, to, ct);

        await Task.WhenAll(providerTask, channelTask);

        // Unwrap the wrapped module envelopes (.Body) — never surface { header, body } to the FE.
        var provider = (await providerTask).Body;
        var channel  = (await channelTask).Body;

        var merged = new MessagingReportsBffResponse(
            ProviderResponseTime: provider?.Items ?? new List<ProviderResponseTimeItem>(),
            ChannelUsage: new ChannelUsageBff(
                ByChannel:  channel?.ByChannel  ?? new List<ChannelVolumeItem>(),
                DailyTrend: channel?.DailyTrend ?? new List<DailyMessageVolumeItem>(),
                PeakHours:  channel?.PeakHours  ?? new List<PeakHourItem>()),
            GeneratedAt: DateTimeOffset.UtcNow);

        return SetResponse(merged);
    }
}
