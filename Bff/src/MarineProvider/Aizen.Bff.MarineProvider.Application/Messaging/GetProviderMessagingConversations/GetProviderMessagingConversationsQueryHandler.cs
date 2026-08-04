using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Messaging;

/// <summary>
/// Resolves the provider identity server-side, then reads the participant-scoped conversation list from the
/// Messaging module (the asserted <c>X-Aizen-User-Id</c> scopes it) and maps Messaging summaries onto the provider
/// inbox shape. Some SR-only display fields are not carried by the Messaging read model in this interim phase —
/// see the field comments; they degrade gracefully and are called out in the report.
/// </summary>
public sealed class GetProviderMessagingConversationsQueryHandler
    : AizenQueryHandler<GetProviderMessagingConversationsQuery, GetProviderConversationsResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IMessagingRemoteCall _messaging;
    private readonly ILogger<GetProviderMessagingConversationsQueryHandler> _logger;

    public GetProviderMessagingConversationsQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IMessagingRemoteCall messaging,
        ILogger<GetProviderMessagingConversationsQueryHandler> logger)
    {
        _resolver       = resolver;
        _identityHolder = identityHolder;
        _messaging      = messaging;
        _logger         = logger;
    }

    public override async Task<GetProviderConversationsResponse?> Handle(
        GetProviderMessagingConversationsQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        // The Messaging scope IS the asserted user id — guard on it so the assertion header is actually attached
        // (the delegating handler only asserts when holder.UserId > 0). No user id crosses the wire.
        if (_identityHolder.UserId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _messaging.GetMyConversations(
                MessagingContextType.ServiceRequest, request.Skip, request.Take);

            var items = result.Body?.Items ?? new();

            var conversations = items
                .Select(MapConversation)
                .Where(c => c.ServiceRequestId > 0) // ignore any non-SR / unparseable context defensively
                .ToList();

            return new GetProviderConversationsResponse { Conversations = conversations };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Messaging GetMyConversations failed for provider user {UserId}",
                _identityHolder.UserId);
            throw new AizenBusinessException("Conversations could not be loaded.");
        }
    }

    private static ProviderConversationDto MapConversation(ConversationSummaryDto s) => new()
    {
        ServiceRequestId   = long.TryParse(s.ContextId, out var srId) ? srId : 0,
        // Messaging read model doesn't carry the SR request code / lifecycle / per-provider unread / last-type.
        // Interim: title + preview + timestamp drive the inbox; the rest degrade until write-cutover (Phase 4).
        RequestCode        = string.Empty,
        Title              = s.Title,
        LastMessagePreview = string.IsNullOrEmpty(s.Preview) ? null : s.Preview,
        LastMessageType    = 1,                 // Text — Messaging summary has no last-message type
        LastMessageAt      = s.Timestamp.UtcDateTime,
        UnreadCount        = 0,                 // Messaging tracks admin-unread only; per-provider unread is Phase 4
        ChannelOpen        = !string.Equals(s.Status, "Closed", StringComparison.OrdinalIgnoreCase),
        LifecycleStatus    = null,              // SR lifecycle status not in the Messaging read model
    };
}
