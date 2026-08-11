using System.Text.Json;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// Provider sends a free-text / location / image message. The anti-harassment gate (SR_MSG_CHANNEL_LOCKED when the
/// customer hasn't replied) is enforced module-side. BFF sets SenderType=Provider via assertion.
///
/// BE_WC2 — behind <c>Messaging:WriteCutover:ChatMessages</c> (default OFF): when ON, TEXT + LOCATION write natively to
/// the Messaging store (resolve the SR's conversation → Messaging SendMessage, which runs the mirrored provider gate);
/// IMAGE still routes to the SR path (WC3 owns images). OFF ⇒ the SR path for everything (sync mirrors to Messaging).
/// </summary>
public sealed class SendProviderMessageCommandHandler
    : AizenCommandHandler<SendProviderMessageCommand, SendServiceRequestMessageResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IMessagingRemoteCall _messaging;
    private readonly IConfiguration _config;
    private readonly ILogger<SendProviderMessageCommandHandler> _logger;

    public SendProviderMessageCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        IMessagingRemoteCall messaging,
        IConfiguration config,
        ILogger<SendProviderMessageCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _messaging = messaging;
        _config = config;
        _logger = logger;
    }

    public override async Task<SendServiceRequestMessageResponse?> Handle(SendProviderMessageCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var hasImage    = request.AttachmentFileId is { } fid && fid != Guid.Empty;
        var hasLocation = request.LocationLat.HasValue && request.LocationLng.HasValue;

        // BE_WC2 write flip: text/location → Messaging (flag ON); image always → SR (until WC3).
        var writeToMessaging = _config.GetValue("Messaging:WriteCutover:ChatMessages", false) && !hasImage;
        if (writeToMessaging)
        {
            var sent = await TrySendViaMessagingAsync(request, hasLocation, ct);
            if (sent is not null)
                return sent;
            // Conversation not resolvable yet → fall through to the SR path (bootstraps via the sync consumer).
        }

        try
        {
            var result = await _serviceRequest.SendMessage(request.ServiceRequestId,
                new SendServiceRequestMessageRequest
                {
                    Content = request.Content,
                    AttachmentFileId = request.AttachmentFileId,
                    SenderTypeOverride = ServiceRequestMessageSenderType.Provider,
                    LocationLat = request.LocationLat,
                    LocationLng = request.LocationLng,
                    LocationLabel = request.LocationLabel
                });
            return result.Body;
        }
        catch (Refit.ApiException ex)
        {
            var message = ExtractMessage(ex.Content);
            _logger.LogWarning(ex, "SendMessage failed for SR {ServiceRequestId}, provider {ProfileId}: {Message}",
                request.ServiceRequestId, _identityHolder.ProfileId, message);
            throw new AizenBusinessException(message ?? "Failed to send message.");
        }
    }

    /// <summary>Resolve the SR's conversation and send TEXT/LOCATION natively to Messaging (which runs the provider
    /// anti-harassment gate). Returns null if the conversation does not exist yet (caller falls back to the SR path).
    /// The Messaging response (a ChatMessageDto) is mapped back to the SR-shaped response the provider client expects.</summary>
    private async Task<SendServiceRequestMessageResponse?> TrySendViaMessagingAsync(
        SendProviderMessageCommand request, bool hasLocation, CancellationToken ct)
    {
        long conversationId;
        try
        {
            var detail = await _messaging.GetMyConversationByContext(MessagingContextType.ServiceRequest, request.ServiceRequestId);
            var idText = detail?.Body?.Conversation?.Id;
            if (!long.TryParse(idText, out conversationId) || conversationId <= 0)
                return null;
        }
        catch (Refit.ApiException)
        {
            return null; // no participant conversation yet → SR-path bootstrap
        }

        SendMessageRequest body;
        if (hasLocation)
        {
            var label = string.IsNullOrWhiteSpace(request.LocationLabel) ? null : request.LocationLabel!.Trim();
            body = new SendMessageRequest(
                Content: LocationJson(request.LocationLat!.Value, request.LocationLng!.Value, label),
                Type: MessageType.Location,
                LocationLat: request.LocationLat,
                LocationLng: request.LocationLng,
                LocationLabel: label);
        }
        else
        {
            body = new SendMessageRequest(Content: request.Content, Type: MessageType.Text);
        }

        try
        {
            var resp = await _messaging.SendMessage(conversationId, body);
            var m = resp?.Body?.Message
                ?? throw new AizenBusinessException("Could not send the message.");
            return new SendServiceRequestMessageResponse(MapToSrDto(m));
        }
        catch (Refit.ApiException ex)
        {
            // The Messaging gate throws AizenBusinessException("SR_MSG_CHANNEL_LOCKED"); surface it like the SR path.
            var message = ExtractMessage(ex.Content);
            _logger.LogWarning(ex, "Messaging SendMessage failed for SR {ServiceRequestId}, provider {ProfileId}: {Message}",
                request.ServiceRequestId, _identityHolder.ProfileId, message);
            throw new AizenBusinessException(message ?? "Failed to send message.");
        }
    }

    private static ServiceRequestMessageDto MapToSrDto(ChatMessageDto m) => new()
    {
        Id            = long.TryParse(m.Id, out var mid) ? mid : 0,
        SenderUserId  = long.TryParse(m.SenderUserId, out var sid) ? sid : 0,
        SenderType    = ServiceRequestMessageSenderType.Provider,
        MessageType   = m.Location is not null ? ServiceRequestMessageType.Location : ServiceRequestMessageType.Text,
        Content       = m.Content,
        AttachmentFileId = null,
        LocationLat   = m.Location is { } loc ? (decimal?)loc.Lat : null,
        LocationLng   = m.Location is { } loc2 ? (decimal?)loc2.Lng : null,
        LocationLabel = m.Location?.Label,
        IsRead        = false,
        CreatedAt     = m.Timestamp.UtcDateTime,
    };

    private static string LocationJson(decimal lat, decimal lng, string? label)
        => JsonSerializer.Serialize(new { lat = (double)lat, lng = (double)lng, label = label ?? string.Empty });

    private static string? ExtractMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("header", out var h) && h.TryGetProperty("errorMessage", out var m))
                return m.GetString();
        }
        catch { }
        return null;
    }
}
