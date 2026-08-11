using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

/// <summary>
/// BFF → Messaging module participant-scoped reads (BE_MO10a). Mirrors the provider's <c>IMessagingRemoteCall</c>: the
/// caller is resolved from the assertion (the delegating handler injects <c>X-Aizen-User-Id</c>), so <c>/mine</c> and
/// <c>/by-context/mine</c> scope to the owner's own conversations — **no user id on the wire**. The by-context read is
/// authorized module-side (the caller must be a conversation participant), so an owner can only read their own threads
/// with no BFF-side gate.
/// </summary>
public interface IMessagingRemoteCall : IAizenRemoteCall
{
    // The caller's conversation inbox (optionally filtered to one context type; paged).
    [AizenRemoteCallGet("/api/v1/conversations/mine")]
    Task<AizenApiResponse<GetConversationListResponse>> GetMyConversations(
        [Refit.Query] MessagingContextType? contextType, [Refit.Query] int skip, [Refit.Query] int take);

    // The caller's conversation thread for one context (e.g. a service request) — 403 module-side if not a participant.
    [AizenRemoteCallGet("/api/v1/conversations/by-context/mine")]
    Task<AizenApiResponse<GetConversationDetailResponse>> GetMyConversationByContext(
        [Refit.Query] MessagingContextType contextType, [Refit.Query] long contextId);

    // BE_WC2 — participant-scoped native send (owner writes text/location to the Messaging store). The module resolves
    // the sender (Owner) + participant membership from the asserted user id; the owner is always a participant.
    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages")]
    Task<AizenApiResponse<SendMessageResponse>> SendMessage(
        long conversationId,
        [AizenRemoteCallBody] SendMessageRequest body);
}
