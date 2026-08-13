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
