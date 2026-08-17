using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Application.Command.EnsureConversation;
using Aizen.Modules.Messaging.Application.Query.GetConversationTranscriptByContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Messaging.Controller.V1.Internal;

/// <summary>
/// BE_WC3b — internal (service-to-service) Messaging endpoints. Called by other Inktavia modules over the cluster
/// network (not by end users); the <c>api/v1/messaging/internal</c> prefix must be excluded at the public API gateway.
///
/// <b>Auth:</b> <see cref="AllowAnonymousAttribute"/>, matching the established cluster-internal read pattern used by
/// ReferenceData's read controllers — module hosts attach no outbound service token, so a token-gated endpoint would
/// simply 401 the caller. The endpoint is <b>not</b> participant-scoped by design: the consuming module (e.g. the SR
/// dispute case) enforces its own access at its own layer, exactly as it did when it read <c>sr.Messages</c> directly.
/// It is read-only and returns only chat content already visible to the conversation's participants.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/messaging/internal")]
[Tags("Messaging - Internal (S2S)")]
[DocumentationInfo("Messaging internal controller",
    "Service-to-service read endpoints (cluster-internal, gateway-excluded).")]
public sealed class MessagingInternalController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MessagingInternalController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>
    /// GET the ordered chat transcript for a conversation resolved by domain context (e.g. a ServiceRequest). Used by
    /// the SR module to compose the dispute case transcript from the canonical Messaging store. An SR with no chat yet
    /// yields an empty transcript (never 404/500).
    /// </summary>
    [HttpGet("conversations/by-context/transcript")]
    [ProducesResponseType(typeof(ConversationTranscriptResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ConversationTranscriptResponse?>> GetTranscriptByContext(
        [FromQuery] MessagingContextType contextType,
        [FromQuery] long contextId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ConversationTranscriptResponse>(
            new GetConversationTranscriptByContextQuery(contextType, contextId), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// BE_WC4a — idempotent get-or-create of the ServiceRequest conversation (owner + accepted-provider participants
    /// resolved server-side). Called by the owner/provider BFF send handlers before <c>SendMessage</c> so the very first
    /// chat on a fresh SR creates the conversation natively (no SR bootstrap). Repeated calls return the same id
    /// (Created=false). ConversationId=0 when the SR context can't be resolved → the BFF falls back to the SR path.
    /// </summary>
    [HttpPost("conversations/ensure-by-context")]
    [ProducesResponseType(typeof(EnsureConversationByContextResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<EnsureConversationByContextResponse?>> EnsureByContext(
        [FromQuery] MessagingContextType contextType,
        [FromQuery] long contextId,
        CancellationToken ct = default)
    {
        // WC4a resolves participants from the SR schema, so only the ServiceRequest context is supported here.
        if (contextType != MessagingContextType.ServiceRequest)
            return SetResponse<EnsureConversationByContextResponse>(new() { ConversationId = 0, Created = false });

        var result = await _cqrs.ProcessAsync<EnsureConversationByContextResponse>(
            new EnsureServiceRequestConversationCommand(contextId), ct);
        return SetResponse(result);
    }
}
