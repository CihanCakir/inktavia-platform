using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Application.Command.CreateConversation;
using Aizen.Modules.Messaging.Application.Command.CreateSupportRequest;
using Aizen.Modules.Messaging.Application.Query.GetConversationByContext;
using Aizen.Modules.Messaging.Application.Query.GetConversationDetail;
using Aizen.Modules.Messaging.Application.Query.GetConversationList;
using Aizen.Modules.Messaging.Application.Query.GetMyConversationByContext;
using Aizen.Modules.Messaging.Application.Query.GetMyConversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Messaging.Controller.V1.Conversations;

[ApiController]
[Route("api/v1/conversations")]
[Tags("Messaging - Conversations")]
[Authorize]
[DocumentationInfo("Conversations controller",
    "Manages conversation creation, listing, and detail retrieval across all context types.")]
public sealed class ConversationsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ConversationsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    // The detail endpoint is the admin-facing read path (participant apps use by-context). Admins must see
    // internal notes and moderated/blocked messages (redacted); non-admins must not. Mirror the same role
    // check ServiceRequestMessageController uses.
    private bool IsAdmin => ContextAccessor.HttpContext!.User.IsInRole("Admin");

    [HttpGet]
    [ProducesResponseType(typeof(GetConversationListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationListResponse?>> GetList(
        [FromQuery] ConversationStatus? status,
        [FromQuery] MessagingContextType? contextType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationListResponse>(
            new GetConversationListQuery(status, contextType, skip, take), ct);
        return SetResponse(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse?>> GetDetail(
        [FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationDetailResponse>(
            new GetConversationDetailQuery(id, IsAdmin), ct);
        return SetResponse(result);
    }

    [HttpGet("by-context")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse?>> GetByContext(
        [FromQuery] MessagingContextType contextType,
        [FromQuery] long contextId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationDetailResponse>(
            new GetConversationByContextQuery(contextType, contextId), ct);
        return SetResponse(result);
    }

    // ── PARTICIPANT-SCOPED read path (Phase 3 provider read cutover) ──
    // These are the endpoints participant apps (provider portal via its BFF) call. Scope = the authenticated
    // caller (resolved in the handler from the request principal); no user id is ever accepted from the client,
    // so a caller can only ever see conversations it participates in. The admin unscoped GetList above is unchanged.

    [HttpGet("mine")]
    [ProducesResponseType(typeof(GetConversationListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationListResponse?>> GetMine(
        [FromQuery] MessagingContextType? contextType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationListResponse>(
            new GetMyConversationsQuery(contextType, skip, take), ct);
        return SetResponse(result);
    }

    [HttpGet("by-context/mine")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse?>> GetMineByContext(
        [FromQuery] MessagingContextType contextType,
        [FromQuery] long contextId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationDetailResponse>(
            new GetMyConversationByContextQuery(contextType, contextId), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateConversationResponse), StatusCodes.Status201Created)]
    public async Task<AizenApiResponse<CreateConversationResponse?>> Create(
        [FromBody] CreateConversationRequest request, CancellationToken ct = default)
    {
        var participants = request.Participants
            .Select(p => new ConversationParticipantInput(p.UserId, p.DisplayName, p.Role))
            .ToList();
        var command = new CreateConversationCommand(
            request.ContextType, request.ContextId, request.Title, participants);
        var result = await _cqrs.ProcessAsync<CreateConversationResponse>(command, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/conversations/support — open (or reuse) a live-support conversation (N-D).</summary>
    [HttpPost("support")]
    [ProducesResponseType(typeof(CreateSupportRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateSupportRequestResponse?>> CreateSupportRequest(
        [FromBody] CreateSupportRequestRequest request, CancellationToken ct = default)
    {
        var command = new CreateSupportRequestCommand
        {
            Topic                = request.Topic,
            Subject              = request.Subject,
            FirstMessage         = request.FirstMessage,
            RequesterDisplayName = string.IsNullOrWhiteSpace(request.RequesterDisplayName) ? "Kullanıcı" : request.RequesterDisplayName!,
            RequesterRole        = request.RequesterRole ?? MessagingParticipantRole.Owner,
        };
        var result = await _cqrs.ProcessAsync<CreateSupportRequestResponse>(command, ct);
        return SetResponse(result);
    }
}
