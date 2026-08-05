using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Admin messaging BFF remote call",
    "Defines BFF-to-Messaging module calls. Auth headers (Authorization service token + optional X-Aizen-Bff-Assertion) are " +
    "injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface IAdminMessagingBffRemoteCall : IAizenRemoteCall
{
    // ─── Conversations ────────────────────────────────────────────────────────

    // The Messaging module controllers return the AizenApiResponse envelope ({ header, body }) via SetResponse —
    // unlike Payment's admin controllers which return the payload bare. So these remote calls must deserialize the
    // ENVELOPE (AizenApiResponse<T>); returning the bare T made Refit find no top-level fields → empty/total:0.
    // Mirrors IVesselAdminBffRemoteCall (the Vessel module wraps the same way). The controller unwraps .Body.
    [AizenRemoteCallGet("/api/v1/conversations")]
    Task<AizenApiResponse<GetConversationListResponse>> GetConversationsAsync(
        [Query] string? status,
        [Query] string? contextType,
        [Query] int     skip,
        [Query] int     take,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/conversations/{id}")]
    Task<AizenApiResponse<GetConversationDetailResponse>> GetConversationAsync(
        long id,
        CancellationToken ct = default);

    // ─── Messages ─────────────────────────────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages")]
    Task<AizenApiResponse<SendMessageResponse>> SendMessageAsync(
        long conversationId,
        [AizenRemoteCallBody] SendMessageRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPatch("/api/v1/conversations/{conversationId}/messages/mark-read")]
    Task MarkReadAsync(
        long conversationId,
        CancellationToken ct = default);

    // Same wrapped-vs-bare lesson as the conversation/reporting calls: the module returns the
    // AizenApiResponse envelope via SetResponse, so this must deserialize AizenApiResponse<T>.
    // The previous Task<object> surfaced the whole { header, body } envelope → the FE 500'd. The
    // controller passes the envelope straight through (no re-wrap).
    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages/attachment-upload-url")]
    Task<AizenApiResponse<RequestAttachmentUploadUrlResponseBff>> GetAttachmentUploadUrlAsync(
        long conversationId,
        [AizenRemoteCallBody] AttachmentUploadUrlRequest body,
        CancellationToken ct = default);

    // ─── Moderation ───────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/moderation/queue")]
    Task<AizenApiResponse<GetModerationQueueResponse>> GetModerationQueueAsync(
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

    // Same wrapped-vs-bare lesson as the conversation/moderation calls above: MessagingReportingController
    // returns the AizenApiResponse envelope via SetResponse, so these must deserialize AizenApiResponse<T>
    // (returning bare object silently yielded the whole { header, body } envelope to the FE). The controller
    // unwraps each .Body and merges them into MessagingReportsBffResponse.
    [AizenRemoteCallGet("/api/v1/reporting/messaging/provider-response-time")]
    Task<AizenApiResponse<ProviderResponseTimeReportBody>> GetProviderResponseTimeAsync(
        [Query] string? from,
        [Query] string? to,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/reporting/messaging/channel-usage")]
    Task<AizenApiResponse<ChannelUsageReportBody>> GetChannelUsageAsync(
        [Query] string? from,
        [Query] string? to,
        CancellationToken ct = default);
}
