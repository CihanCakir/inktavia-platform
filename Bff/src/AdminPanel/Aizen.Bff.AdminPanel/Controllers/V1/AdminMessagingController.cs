using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
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
public sealed class AdminMessagingController : AizenWebApiController
{
    private readonly IAdminMessagingBffRemoteCall               _messaging;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public AdminMessagingController(
        IHttpContextAccessor httpContextAccessor,
        IAdminMessagingBffRemoteCall messaging,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
        : base(httpContextAccessor)
    {
        _messaging            = messaging;
        _serviceTokenProvider = serviceTokenProvider;
    }

    // ── Auth helpers ──────────────────────────────────────────────────────────

    private string GetUserToken()
    {
        var raw = HttpContext.Request.Headers.Authorization.ToString();
        return raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? raw["Bearer ".Length..]
            : raw;
    }

    private async Task<(string bearer, string userToken)> GetAuthAsync(CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        return ($"Bearer {serviceToken}", GetUserToken());
    }

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
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _messaging.GetConversationsAsync(status, contextType, skip, take, bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/messaging/conversations/{id}</summary>
    [HttpGet("conversations/{id:long}")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse>> GetConversation(
        long id,
        CancellationToken ct = default)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _messaging.GetConversationAsync(id, bearer, userToken, ct);
        return SetResponse(result);
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
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _messaging.SendMessageAsync(id, body, bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>PATCH api/v1/admin-panel/messaging/conversations/{id}/mark-read</summary>
    [HttpPatch("conversations/{id:long}/mark-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct = default)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        await _messaging.MarkReadAsync(id, bearer, userToken, ct);
        return NoContent();
    }

    /// <summary>POST api/v1/admin-panel/messaging/conversations/{id}/attachment-upload-url</summary>
    [HttpPost("conversations/{id:long}/attachment-upload-url")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> GetAttachmentUploadUrl(
        long id,
        [FromBody] AttachmentUploadUrlRequest body,
        CancellationToken ct = default)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _messaging.GetAttachmentUploadUrlAsync(id, body, bearer, userToken, ct);
        return SetResponse(result);
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
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _messaging.GetModerationQueueAsync(skip, take, bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>PATCH api/v1/admin-panel/messaging/moderation/messages/{id}</summary>
    [HttpPatch("moderation/messages/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ModerateMessage(
        long id,
        [FromBody] ModerateMessageRequest body,
        CancellationToken ct = default)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        await _messaging.ModerateMessageAsync(id, body, bearer, userToken, ct);
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
        var (bearer, userToken) = await GetAuthAsync(ct);
        await _messaging.FlagConversationAsync(id, body, bearer, userToken, ct);
        return NoContent();
    }

    // ─── Reporting ────────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/v1/admin-panel/messaging/reports
    /// Merges provider-response-time and channel-usage into one response object.
    /// </summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> GetReports(
        [FromQuery] string? from = null,
        [FromQuery] string? to   = null,
        CancellationToken ct     = default)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var providerTask = _messaging.GetProviderResponseTimeAsync(from, to, bearer, userToken, ct);
        var channelTask  = _messaging.GetChannelUsageAsync(from, to, bearer, userToken, ct);

        await Task.WhenAll(providerTask, channelTask);

        var merged = (object)new
        {
            providerResponseTime = await providerTask,
            channelUsage         = await channelTask,
            generatedAt          = DateTimeOffset.UtcNow.ToString("O")
        };

        return SetResponse(merged);
    }
}
