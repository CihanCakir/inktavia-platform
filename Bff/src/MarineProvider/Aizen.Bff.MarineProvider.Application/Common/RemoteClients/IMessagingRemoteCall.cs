using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Enum;
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
}
