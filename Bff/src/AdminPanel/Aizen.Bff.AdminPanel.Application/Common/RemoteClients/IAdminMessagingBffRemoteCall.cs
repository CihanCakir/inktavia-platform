using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Admin messaging BFF remote call",
    "Defines BFF-to-Messaging module calls. Auth headers (Authorization + X-Aizen-User-Token) are " +
    "injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface IAdminMessagingBffRemoteCall : IAizenRemoteCall
{
    // ─── Conversations ────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/conversations")]
    Task<GetConversationListResponse> GetConversationsAsync(
        [Query] string? status,
        [Query] string? contextType,
        [Query] int     skip,
        [Query] int     take,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/conversations/{id}")]
    Task<GetConversationDetailResponse> GetConversationAsync(
        long id,
        CancellationToken ct = default);

    // ─── Messages ─────────────────────────────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages")]
    Task<SendMessageResponse> SendMessageAsync(
        long conversationId,
        [AizenRemoteCallBody] SendMessageRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPatch("/api/v1/conversations/{conversationId}/messages/mark-read")]
    Task MarkReadAsync(
        long conversationId,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages/attachment-upload-url")]
    Task<object> GetAttachmentUploadUrlAsync(
        long conversationId,
        [AizenRemoteCallBody] AttachmentUploadUrlRequest body,
        CancellationToken ct = default);

    // ─── Moderation ───────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/moderation/queue")]
    Task<GetModerationQueueResponse> GetModerationQueueAsync(
        [Query] int skip,
        [Query] int take,
        CancellationToken ct = default);

    [AizenRemoteCallPatch("/api/v1/moderation/messages/{messageId}")]
    Task ModerateMessageAsync(
        long messageId,
        [AizenRemoteCallBody] ModerateMessageRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPatch("/api/v1/moderation/conversations/{conversationId}/flag")]
    Task FlagConversationAsync(
        long conversationId,
        [AizenRemoteCallBody] FlagConversationRequest body,
        CancellationToken ct = default);

    // ─── Reporting ────────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/reporting/messaging/provider-response-time")]
    Task<object> GetProviderResponseTimeAsync(
        [Query] string? from,
        [Query] string? to,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/reporting/messaging/channel-usage")]
    Task<object> GetChannelUsageAsync(
        [Query] string? from,
        [Query] string? to,
        CancellationToken ct = default);
}
