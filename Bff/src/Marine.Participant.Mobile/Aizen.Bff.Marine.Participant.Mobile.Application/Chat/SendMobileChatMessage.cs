using System.Text.Json;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

/// <summary>POST /api/v1/mobile/service-requests/{id}/messages — the owner sends a message on their own SR: exactly one
/// of { text, image (AttachmentFileId from the reused upload), location (lat/lng[+label]) } (BE_MO10a/b). The
/// anti-harassment gate is provider-only, so the owner always opens the channel. Cost-free.
///
/// BE_WC4b — chat writes go to the Messaging store UNCONDITIONALLY (ensure-then-send creates the conversation natively).
/// The WriteCutover flag + the SR fallback are gone; there is no sr.Messages chat write. Phase-4 complete.</summary>
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
    private readonly IMessagingRemoteCall _messaging;

    public SendMobileChatMessageCommandHandler(
        IParticipantProfileResolver resolver,
        IMessagingRemoteCall messaging)
    {
        _resolver  = resolver;
        _messaging = messaging;
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

        // BE_WC4b — chat writes go to the Messaging store UNCONDITIONALLY. The WriteCutover flag, the SR fallback, and
        // the SR chat write are gone (Phase-4 complete): ensure-then-send creates the conversation natively and writes
        // the message to Messaging. There is no sr.Messages chat write anymore.
        var sent = await TrySendViaMessagingAsync(request.ServiceRequestId, kind, content, r, cancellationToken);
        return sent ?? throw new AizenBusinessException("Could not send the message.");
    }

    /// <summary>Ensure the SR's conversation exists (native get-or-create) and send TEXT/LOCATION/IMAGE natively to
    /// Messaging. Returns null if the conversation can't be ensured (caller falls back to the SR path — belt-and-
    /// suspenders until WC4b). Location rides Content-as-JSON (what the Messaging read/echo <c>ParseLocation</c> expects)
    /// AND the discrete WC0 columns (persisted by the handler).</summary>
    private async Task<MobileChatMessageDto?> TrySendViaMessagingAsync(
        long serviceRequestId, MobileChatSendKind kind, string content,
        MobileSendChatMessageRequest r, CancellationToken cancellationToken)
    {
        // BE_WC4a — ensure (idempotent get-or-create) instead of a read-then-fallback, so the FIRST message on a fresh
        // SR (no prior conversation) creates it NATIVELY on the Messaging side — no SR bootstrap row. Existing
        // conversations return their id (Created=false). ConversationId<=0 / a failure → fall back to the SR path.
        long conversationId;
        try
        {
            var ensured = await _messaging.EnsureConversationByContext(MessagingContextType.ServiceRequest, serviceRequestId);
            conversationId = ensured?.Body?.ConversationId ?? 0;
            if (conversationId <= 0)
                return null;
        }
        catch (Refit.ApiException)
        {
            return null;
        }

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
