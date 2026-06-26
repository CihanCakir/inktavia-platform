using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Admin messaging BFF remote call",
    "Defines BFF-to-Messaging module calls. All endpoints require explicit Authorization (service token) " +
    "and X-Aizen-User-Token (caller's JWT) headers.")]
public interface IAdminMessagingBffRemoteCall : IAizenRemoteCall
{
    // ─── Conversations ────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/conversations")]
    Task<GetConversationListResponse> GetConversationsAsync(
        [Query] string? status,
        [Query] string? contextType,
        [Query] int     skip,
        [Query] int     take,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/conversations/{id}")]
    Task<GetConversationDetailResponse> GetConversationAsync(
        long id,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);

    // ─── Messages ─────────────────────────────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages")]
    Task<SendMessageResponse> SendMessageAsync(
        long conversationId,
        [AizenRemoteCallBody]                         SendMessageRequest body,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);

    [AizenRemoteCallPatch("/api/v1/conversations/{conversationId}/messages/mark-read")]
    Task MarkReadAsync(
        long conversationId,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages/attachment-upload-url")]
    Task<object> GetAttachmentUploadUrlAsync(
        long conversationId,
        [AizenRemoteCallBody]                         AttachmentUploadUrlRequest body,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);

    // ─── Moderation ───────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/moderation/queue")]
    Task<GetModerationQueueResponse> GetModerationQueueAsync(
        [Query] int skip,
        [Query] int take,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);

    [AizenRemoteCallPatch("/api/v1/moderation/messages/{messageId}")]
    Task ModerateMessageAsync(
        long messageId,
        [AizenRemoteCallBody]                         ModerateMessageRequest body,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);

    [AizenRemoteCallPatch("/api/v1/moderation/conversations/{conversationId}/flag")]
    Task FlagConversationAsync(
        long conversationId,
        [AizenRemoteCallBody]                         FlagConversationRequest body,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);

    // ─── Reporting ────────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/reporting/messaging/provider-response-time")]
    Task<object> GetProviderResponseTimeAsync(
        [Query] string? from,
        [Query] string? to,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/reporting/messaging/channel-usage")]
    Task<object> GetChannelUsageAsync(
        [Query] string? from,
        [Query] string? to,
        [AizenRemoteCallHeader("Authorization")]       string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        CancellationToken ct = default);
}
