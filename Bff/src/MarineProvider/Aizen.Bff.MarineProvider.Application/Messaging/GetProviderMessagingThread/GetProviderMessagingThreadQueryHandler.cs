using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.ServiceRequests;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Messaging;

/// <summary>
/// Resolves the provider identity, reads the thread from the Messaging module BY CONTEXT (ServiceRequest id) — the
/// module authorizes the asserted caller as a participant — and maps Messaging <c>ChatMessageDto</c> onto the
/// provider <see cref="ServiceRequestMessageDto"/> shape so the FE thread renders unchanged. ChannelOpen mirrors the
/// SR-backed handler exactly (an Owner message means the customer has engaged).
/// </summary>
public sealed class GetProviderMessagingThreadQueryHandler
    : AizenQueryHandler<GetProviderMessagingThreadQuery, ProviderMessagesResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IMessagingRemoteCall _messaging;
    private readonly ILogger<GetProviderMessagingThreadQueryHandler> _logger;

    public GetProviderMessagingThreadQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IMessagingRemoteCall messaging,
        ILogger<GetProviderMessagingThreadQueryHandler> logger)
    {
        _resolver       = resolver;
        _identityHolder = identityHolder;
        _messaging      = messaging;
        _logger         = logger;
    }

    public override async Task<ProviderMessagesResponse?> Handle(
        GetProviderMessagingThreadQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.UserId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _messaging.GetMyConversationByContext(
                MessagingContextType.ServiceRequest, request.ServiceRequestId);

            var messages = (result.Body?.Conversation?.Messages ?? new())
                .Select(MapMessage)
                .ToList();

            var channelOpen = messages.Any(m => m.SenderType == ServiceRequestMessageSenderType.Owner);

            return new ProviderMessagesResponse
            {
                Items       = messages,
                ChannelOpen = channelOpen,
                TotalCount  = messages.Count,
            };
        }
        catch (Refit.ApiException ex)
        {
            // 403 (not a participant) or 404 (no conversation yet) both surface as "not found" to the provider,
            // exactly like the SR-backed handler — no id-space probing signal.
            _logger.LogWarning(ex, "Messaging thread failed for SR {ServiceRequestId}, provider user {UserId}",
                request.ServiceRequestId, _identityHolder.UserId);
            throw new AizenBusinessException("Service request not found.");
        }
    }

    private static ServiceRequestMessageDto MapMessage(ChatMessageDto m) => new()
    {
        Id               = long.TryParse(m.Id, out var id) ? id : 0,
        SenderUserId     = long.TryParse(m.SenderUserId, out var uid) ? uid : 0,
        SenderType       = MapSenderType(m.SenderRole),
        MessageType      = MapMessageType(m.Type),
        Content          = m.Content,
        // Messaging read model has no per-participant read state in this phase; treat rendered thread as read to
        // avoid spurious "unread" indicators. Per-message read receipts are deferred (Phase 4).
        IsRead           = true,
        ReadAt           = null,
        AttachmentFileId = ParseAttachment(m.Attachments),
        LocationLat      = m.Location is not null ? (decimal)m.Location.Lat : null,
        LocationLng      = m.Location is not null ? (decimal)m.Location.Lng : null,
        LocationLabel    = m.Location?.Label,
        CreatedAt        = m.Timestamp.UtcDateTime,
    };

    // Messaging participant roles and SR sender types share the same names for the values that appear in an SR
    // thread (Owner/Provider/Admin/System), so a name parse maps them 1:1; anything else falls back to System.
    private static ServiceRequestMessageSenderType MapSenderType(string role)
        => Enum.TryParse<ServiceRequestMessageSenderType>(role, ignoreCase: true, out var st)
            ? st
            : ServiceRequestMessageSenderType.System;

    private static ServiceRequestMessageType MapMessageType(string type) => type switch
    {
        "Text"               => ServiceRequestMessageType.Text,
        "SystemNotification" => ServiceRequestMessageType.SystemNotification,
        "StatusChange"       => ServiceRequestMessageType.StatusChange,
        "MediaAttachment"    => ServiceRequestMessageType.Image,
        "Location"           => ServiceRequestMessageType.Location,
        _                    => ServiceRequestMessageType.Text,
    };

    private static Guid? ParseAttachment(IEnumerable<AttachmentDto> attachments)
    {
        var url = attachments.FirstOrDefault()?.Url;
        return Guid.TryParse(url, out var g) ? g : null;
    }
}
