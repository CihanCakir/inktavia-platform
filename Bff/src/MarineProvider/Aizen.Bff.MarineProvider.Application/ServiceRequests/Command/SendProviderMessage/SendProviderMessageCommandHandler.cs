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
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
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
    private readonly IMessagingRemoteCall _messaging;
    private readonly ILogger<SendProviderMessageCommandHandler> _logger;

    public SendProviderMessageCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IMessagingRemoteCall messaging,
        ILogger<SendProviderMessageCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _messaging = messaging;
        _logger = logger;
    }

    public override async Task<SendServiceRequestMessageResponse?> Handle(SendProviderMessageCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var hasImage    = request.AttachmentFileId is { } fid && fid != Guid.Empty;
        var hasLocation = request.LocationLat.HasValue && request.LocationLng.HasValue;

        // BE_WC4b — chat writes go to the Messaging store UNCONDITIONALLY. The WriteCutover flag, the SR fallback, and
        // the SR chat write are gone (Phase-4 complete): ensure-then-send creates the conversation natively and writes
        // the message to Messaging (the provider anti-harassment gate runs module-side inside Messaging SendMessage).
        var sent = await TrySendViaMessagingAsync(request, hasLocation, hasImage, ct);
        return sent ?? throw new AizenBusinessException("Could not send the message.");
    }

    /// <summary>Resolve the SR's conversation and send TEXT / LOCATION / IMAGE natively to Messaging (which runs the
    /// provider anti-harassment gate). Returns null if the conversation does not exist yet (caller falls back to the SR
    /// path). The Messaging response (a ChatMessageDto) is mapped back to the SR-shaped response the client expects.</summary>
    private async Task<SendServiceRequestMessageResponse?> TrySendViaMessagingAsync(
        SendProviderMessageCommand request, bool hasLocation, bool hasImage, CancellationToken ct)
    {
        // BE_WC4a — ensure (idempotent get-or-create) instead of a read-then-fallback, so the FIRST message on a fresh
        // SR (no prior conversation) creates it NATIVELY on the Messaging side — no SR bootstrap row. Existing
        // conversations return their id. The provider anti-harassment gate stays module-side inside SendMessage below.
        long conversationId;
        try
        {
            var ensured = await _messaging.EnsureConversationByContext(MessagingContextType.ServiceRequest, request.ServiceRequestId);
            conversationId = ensured?.Body?.ConversationId ?? 0;
            if (conversationId <= 0)
                return null; // couldn't ensure → SR-path fallback (belt-and-suspenders until WC4b)
        }
        catch (Refit.ApiException)
        {
            return null;
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
        else if (hasImage)
        {
            // BE_WC3a — the image is already uploaded (the provider passed a fileId); attach it directly (no upload
            // session), stored as FileStorageId = fileId.ToString() exactly like the SR sync mirror.
            body = new SendMessageRequest(
                Content: string.Empty,
                Type: MessageType.MediaAttachment,
                AttachmentFileStorageId: request.AttachmentFileId!.Value.ToString(),
                AttachmentFileName: "attachment",
                AttachmentFileType: "image");
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

    private static ServiceRequestMessageDto MapToSrDto(ChatMessageDto m)
    {
        // BE_WC3a — a Messaging image carries the fileId as the attachment's FileStorageId (echoed as AttachmentDto.Url).
        Guid? attachmentFileId = m.Attachments is { Count: > 0 } && Guid.TryParse(m.Attachments[0].Url, out var g) ? g : null;
        var messageType = m.Location is not null
            ? ServiceRequestMessageType.Location
            : attachmentFileId is not null ? ServiceRequestMessageType.Image : ServiceRequestMessageType.Text;
        return new ServiceRequestMessageDto
        {
            Id            = long.TryParse(m.Id, out var mid) ? mid : 0,
            SenderUserId  = long.TryParse(m.SenderUserId, out var sid) ? sid : 0,
            SenderType    = ServiceRequestMessageSenderType.Provider,
            MessageType   = messageType,
            Content       = m.Content,
            AttachmentFileId = attachmentFileId,
            LocationLat   = m.Location is { } loc ? (decimal?)loc.Lat : null,
            LocationLng   = m.Location is { } loc2 ? (decimal?)loc2.Lng : null,
            LocationLabel = m.Location?.Label,
            IsRead        = false,
            CreatedAt     = m.Timestamp.UtcDateTime,
        };
    }

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
