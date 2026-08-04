using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Messaging;
using Aizen.Bff.MarineProvider.Application.ServiceRequests;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

/// <summary>
/// Phase 3 — provider READS conversations/threads from the unified Messaging module. WRITES still go to the
/// ServiceRequest module (see <see cref="ServiceRequestsController"/> POST .../messages). Every read here is scoped
/// server-side to the resolved provider (the handler asserts the provider user id; the Messaging module authorizes
/// the participant) — a provider can never read another provider's conversations.
/// </summary>
[ApiController]
[Route("api/v1/provider/messaging")]
[Tags("Provider - Messaging (unified read)")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class ProviderMessagingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderMessagingController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>Provider inbox — conversations from the Messaging store, scoped to this provider.</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(GetProviderConversationsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetProviderConversationsResponse?>> GetConversations(
        [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderMessagingConversationsQuery
        {
            Skip = skip, Take = take,
        }, ct));

    /// <summary>A single thread, keyed by service-request id, from the Messaging store (participant-authorized).</summary>
    [HttpGet("service-requests/{serviceRequestId:long}/thread")]
    [ProducesResponseType(typeof(ProviderMessagesResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderMessagesResponse?>> GetThread(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderMessagingThreadQuery
        {
            ServiceRequestId = serviceRequestId,
        }, ct));
}
