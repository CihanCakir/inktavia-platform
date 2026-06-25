using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Admin messaging BFF remote call", "Refit interface for BFF-to-Messaging module communication. Auth forwarded via AuthorizationForwardingHandler.")]
public interface IAdminMessagingBffRemoteCall : IAizenRemoteCall
{
    // ─── Conversations ────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/conversations</summary>
    [Get("/api/v1/conversations")]
    Task<GetConversationListResponse> GetConversationsAsync(
        [Query] string? status      = null,
        [Query] string? contextType = null,
        [Query] int     skip        = 0,
        [Query] int     take        = 20,
        CancellationToken ct        = default);

    /// <summary>GET /api/v1/conversations/{id}</summary>
    [Get("/api/v1/conversations/{id}")]
    Task<GetConversationDetailResponse> GetConversationAsync(
        long id,
        CancellationToken ct = default);

    // ─── Messages ─────────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/conversations/{conversationId}/messages</summary>
    [Post("/api/v1/conversations/{conversationId}/messages")]
    Task<SendMessageResponse> SendMessageAsync(
        long conversationId,
        [Body] SendMessageRequest body,
        CancellationToken ct = default);

    /// <summary>PATCH /api/v1/conversations/{conversationId}/messages/mark-read</summary>
    [Patch("/api/v1/conversations/{conversationId}/messages/mark-read")]
    Task MarkReadAsync(
        long conversationId,
        CancellationToken ct = default);

    /// <summary>POST /api/v1/conversations/{conversationId}/messages/attachment-upload-url</summary>
    [Post("/api/v1/conversations/{conversationId}/messages/attachment-upload-url")]
    Task<object> GetAttachmentUploadUrlAsync(
        long conversationId,
        [Body] AttachmentUploadUrlRequest body,
        CancellationToken ct = default);

    // ─── Moderation ───────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/moderation/queue</summary>
    [Get("/api/v1/moderation/queue")]
    Task<GetModerationQueueResponse> GetModerationQueueAsync(
        [Query] int skip = 0,
        [Query] int take = 20,
        CancellationToken ct = default);

    /// <summary>PATCH /api/v1/moderation/messages/{messageId}</summary>
    [Patch("/api/v1/moderation/messages/{messageId}")]
    Task ModerateMessageAsync(
        long messageId,
        [Body] ModerateMessageRequest body,
        CancellationToken ct = default);

    /// <summary>PATCH /api/v1/moderation/conversations/{conversationId}/flag</summary>
    [Patch("/api/v1/moderation/conversations/{conversationId}/flag")]
    Task FlagConversationAsync(
        long conversationId,
        [Body] FlagConversationRequest body,
        CancellationToken ct = default);

    // ─── Reporting ────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/reporting/messaging/provider-response-time</summary>
    [Get("/api/v1/reporting/messaging/provider-response-time")]
    Task<object> GetProviderResponseTimeAsync(
        [Query] string? from = null,
        [Query] string? to   = null,
        CancellationToken ct = default);

    /// <summary>GET /api/v1/reporting/messaging/channel-usage</summary>
    [Get("/api/v1/reporting/messaging/channel-usage")]
    Task<object> GetChannelUsageAsync(
        [Query] string? from = null,
        [Query] string? to   = null,
        CancellationToken ct = default);
}
