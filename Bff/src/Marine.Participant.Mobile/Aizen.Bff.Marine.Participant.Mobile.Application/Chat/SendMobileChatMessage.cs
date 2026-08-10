using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

/// <summary>POST /api/v1/mobile/service-requests/{id}/messages — the owner sends a message on their own SR: exactly one
/// of { text, image (AttachmentFileId from the reused upload), location (lat/lng[+label]) } (BE_MO10a/b). Reuses the SR
/// module send with SenderType=Owner (identity from the token; never the body). The anti-harassment gate is
/// provider-only, so the owner always opens the channel. Mirrors SendProviderMessage minus the gate. Cost-free.</summary>
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

    public SendMobileChatMessageCommandHandler(IParticipantProfileResolver resolver, IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _sr = sr;
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
}
