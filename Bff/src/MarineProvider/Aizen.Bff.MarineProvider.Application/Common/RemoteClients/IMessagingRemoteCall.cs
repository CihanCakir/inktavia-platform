using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

/// <summary>
/// BFF -> Messaging module calls, PARTICIPANT-SCOPED (Phase 3 provider read cutover). These hit the Messaging
/// module's <c>/mine</c> endpoints, which scope to the caller resolved from the trusted-BFF assertion header
/// (<c>X-Aizen-User-Id</c>) that <see cref="Http.MarineProviderBffAuthDelegatingHandler"/> injects automatically —
/// so NO user id is ever passed on the wire and a provider can only read its own conversations.
/// The Messaging module wraps every response in <see cref="AizenApiResponse{T}"/>; deserialize the envelope.
/// </summary>
public interface IMessagingRemoteCall : IAizenRemoteCall
{
    /// <summary>Conversations the asserted caller participates in (filtered by context type), newest-message first.</summary>
    [AizenRemoteCallGet("/api/v1/conversations/mine")]
    Task<AizenApiResponse<GetConversationListResponse>> GetMyConversations(
        [Refit.Query] MessagingContextType? contextType = null,
        [Refit.Query] int skip = 0,
        [Refit.Query] int take = 50);

    /// <summary>A single thread by domain context (ServiceRequest id), authorized to the asserted participant.</summary>
    [AizenRemoteCallGet("/api/v1/conversations/by-context/mine")]
    Task<AizenApiResponse<GetConversationDetailResponse>> GetMyConversationByContext(
        [Refit.Query] MessagingContextType contextType,
        [Refit.Query] long contextId);

    // ─── N-D live support (participant-scoped writes; the module resolves the requester from the assertion) ──────
    [AizenRemoteCallPost("/api/v1/conversations/support")]
    Task<AizenApiResponse<CreateSupportRequestResponse>> CreateSupportRequest(
        [AizenRemoteCallBody] CreateSupportRequestRequest body);

    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages")]
    Task<AizenApiResponse<SendMessageResponse>> SendMessage(
        long conversationId,
        [AizenRemoteCallBody] SendMessageRequest body);

    // BE_WC3a — participant-scoped chat-attachment access-check (Authorized iff the caller is a participant AND the
    // fileId is on a message in this SR's conversation). Tried first by the read-url handler; falls back to the SR
    // access-check for request/evidence attachments on a miss.
    [AizenRemoteCallGet("/api/v1/conversations/by-context/mine/attachments/{fileId}/access-check")]
    Task<AizenApiResponse<ChatAttachmentAccessResponse>> CheckChatAttachmentAccess(
        Guid fileId, [Refit.Query] MessagingContextType contextType, [Refit.Query] long contextId);

    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages/attachment-upload-url")]
    Task<AizenApiResponse<SupportAttachmentUploadUrlBff>> GetAttachmentUploadUrl(
        long conversationId,
        [AizenRemoteCallBody] AttachmentUploadUrlRequest body);
}

/// <summary>
/// BFF-local mirror of the Messaging module's RequestAttachmentUploadUrlResponse (which lives in its .Application layer,
/// not referenced by the BFF). Same wrapped-vs-bare lesson as N0 — the remote call must deserialize the envelope.
/// </summary>
public sealed record SupportAttachmentUploadUrlBff(
    System.Guid FileId, string UploadSessionCode, string UploadUrl, System.DateTime ExpiresAt);
