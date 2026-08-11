using System.Text.Json;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Message;
using Microsoft.Extensions.Configuration;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

/// <summary>POST /api/v1/mobile/service-requests/{id}/messages — the owner sends a message on their own SR: exactly one
/// of { text, image (AttachmentFileId from the reused upload), location (lat/lng[+label]) } (BE_MO10a/b). Reuses the SR
/// module send with SenderType=Owner (identity from the token; never the body). The anti-harassment gate is
/// provider-only, so the owner always opens the channel. Mirrors SendProviderMessage minus the gate. Cost-free.
///
/// BE_WC2 — behind <c>Messaging:WriteCutover:ChatMessages</c> (default OFF): when ON, TEXT + LOCATION write natively to
/// the Messaging store (resolve the SR's conversation → Messaging SendMessage); IMAGE still routes to the SR path
/// (WC3 owns the image cutover). OFF ⇒ the SR path for everything (the sync mirrors to Messaging). Reversible.</summary>
public sealed class SendMobileChatMessageCommand : AizenCommand<MobileChatMessageDto>
{
    public SendMobileChatMessageCommand(MobileSendChatMessageRequest request, long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public MobileSendChatMessageRequest Request { get; }
}

public sealed class SendMobileChatMessageCommandHandler
    : AizenCommandHandler<SendMobileChatMessageCommand, MobileChatMessageDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IMessagingRemoteCall _messaging;
    private readonly IConfiguration _config;

    public SendMobileChatMessageCommandHandler(
        IParticipantProfileResolver resolver,
        IServiceRequestRemoteCall sr,
        IMessagingRemoteCall messaging,
        IConfiguration config)
    {
        _resolver  = resolver;
        _sr        = sr;
        _messaging = messaging;
        _config    = config;
    }

    public override async Task<MobileChatMessageDto?> Handle(
        SendMobileChatMessageCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var r = request.Request ?? new MobileSendChatMessageRequest();

        // Exactly one of { text, image, location } + coord range + length caps (the SR module validates none of this).
        var (kind, content) = MobileChatSend.Validate(
            r.Content, r.AttachmentFileId, r.LocationLat, r.LocationLng, r.LocationLabel);

        // BE_WC2/WC3a write flip: text / location / image → Messaging (flag ON). WC3a moved images too, so the flag now
        // routes ALL three kinds natively; flag OFF reverts every kind to the SR path (reversible).
        var writeToMessaging = _config.GetValue("Messaging:WriteCutover:ChatMessages", false);
        if (writeToMessaging)
        {
            var sent = await TrySendViaMessagingAsync(request.ServiceRequestId, kind, content, r, cancellationToken);
            if (sent is not null)
                return sent;
            // Conversation not resolvable yet (no synced/System message) → fall through to the SR path, which
            // bootstraps the conversation via the sync consumer. Reversible-safe.
        }

        // ── SR path (flag OFF, image, or no conversation yet) ────────────────────────────────────────────────
        // Owner identity comes from the assertion; the module stamps the sender id + skips the provider-only gate.
        // The SR entity branches: Location wins over Image wins over Text — we only ever send one kind (validated above).
        var body = new SendServiceRequestMessageRequest
        {
            Content            = content,
            SenderTypeOverride = ServiceRequestMessageSenderType.Owner,
        };
        if (kind == MobileChatSendKind.Image)
        {
            body.AttachmentFileId = r.AttachmentFileId;
        }
        else if (kind == MobileChatSendKind.Location)
        {
            body.LocationLat   = (decimal?)r.LocationLat;
            body.LocationLng   = (decimal?)r.LocationLng;
            body.LocationLabel = string.IsNullOrWhiteSpace(r.LocationLabel) ? null : r.LocationLabel!.Trim();
        }

        var resp = await _sr.SendMessage(request.ServiceRequestId, body);
        var message = resp?.Body?.Message
            ?? throw new AizenBusinessException("Could not send the message.");

        return MobileChatMapper.MapSentMessage(message);
    }

    /// <summary>Resolve the SR's conversation and send TEXT/LOCATION natively to Messaging. Returns null if the
    /// conversation does not exist yet (caller falls back to the SR path). Location rides Content-as-JSON (what the
    /// Messaging read/echo <c>ParseLocation</c> expects) AND the discrete WC0 columns (persisted by the handler).</summary>
    private async Task<MobileChatMessageDto?> TrySendViaMessagingAsync(
        long serviceRequestId, MobileChatSendKind kind, string content,
        MobileSendChatMessageRequest r, CancellationToken cancellationToken)
    {
        var detail = await _messaging.GetMyConversationByContext(MessagingContextType.ServiceRequest, serviceRequestId);
        var idText = detail?.Body?.Conversation?.Id;
        if (!long.TryParse(idText, out var conversationId) || conversationId <= 0)
            return null;

        SendMessageRequest body;
        if (kind == MobileChatSendKind.Location)
        {
            var label = string.IsNullOrWhiteSpace(r.LocationLabel) ? null : r.LocationLabel!.Trim();
            body = new SendMessageRequest(
                Content: LocationJson(r.LocationLat!.Value, r.LocationLng!.Value, label),
                Type: MessageType.Location,
                LocationLat: (decimal?)r.LocationLat,
                LocationLng: (decimal?)r.LocationLng,
                LocationLabel: label);
        }
        else if (kind == MobileChatSendKind.Image)
        {
            // BE_WC3a — the client already uploaded the image (mobile upload session); attach the resulting fileId
            // directly (no Messaging upload session). Stored as FileStorageId = fileId.ToString(), exactly like the SR
            // sync mirror — so the WC3a Messaging read-url check finds it for both old synced and new native images.
            body = new SendMessageRequest(
                Content: string.Empty,
                Type: MessageType.MediaAttachment,
                AttachmentFileStorageId: r.AttachmentFileId?.ToString(),
                AttachmentFileName: "attachment",
                AttachmentFileType: "image");
        }
        else
        {
            body = new SendMessageRequest(Content: content, Type: MessageType.Text);
        }

        var resp = await _messaging.SendMessage(conversationId, body);
        var message = resp?.Body?.Message
            ?? throw new AizenBusinessException("Could not send the message.");
        return MobileChatMapper.MapMessagingMessage(message);
    }

    private static string LocationJson(double lat, double lng, string? label)
        => JsonSerializer.Serialize(new { lat, lng, label = label ?? string.Empty });
}
