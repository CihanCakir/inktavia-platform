using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;

/// <summary>
/// BE_WC3b — net-new outbound call from the ServiceRequest module to the Messaging module's internal (S2S) transcript
/// endpoint. Used by the dispute case to source the chat transcript from the canonical Messaging store instead of
/// <c>sr.Messages</c> (so the case stays complete after WC4 freezes <c>sr.Messages</c>).
///
/// Follows the existing SR remote-call convention (see <see cref="IServiceRequestReferenceDataRemoteCall"/>): local
/// projection DTOs declared inline (no reference to the Messaging module's abstraction) and a stable numeric contract
/// for the role/type fields, so the SR module never depends on Messaging enums. The target endpoint is cluster-internal
/// <c>[AllowAnonymous]</c> (module hosts attach no outbound token — same as the SR→ReferenceData reads), so no auth
/// wiring is required here. BaseUrl is configured via <c>RemoteCalls__IServiceRequestMessagingRemoteCall__BaseUrl</c>.
/// </summary>
public interface IServiceRequestMessagingRemoteCall : IAizenRemoteCall
{
    // contextType is the Messaging MessagingContextType member name (e.g. "ServiceRequest"); the endpoint's enum model
    // binding accepts the name. contextId is the ServiceRequest id.
    [AizenRemoteCallGet("/api/v1/messaging/internal/conversations/by-context/transcript?contextType={contextType}&contextId={contextId}")]
    Task<AizenApiResponse<SrConversationTranscriptDto>> GetConversationTranscript(string contextType, long contextId);
}

/// <summary>BE_WC3b — SR-local projection of the Messaging transcript response (property names match the wire DTO).</summary>
public sealed class SrConversationTranscriptDto
{
    public List<SrTranscriptMessageDto> Messages { get; set; } = new();
}

/// <summary>
/// BE_WC3b — one transcript message. <see cref="SenderRole"/> / <see cref="MessageType"/> are the integer values of the
/// Messaging enums (SenderRole: Owner=1, Provider=2, Admin=3, System=4, Support=5, Buyer=6, Seller=7 · MessageType:
/// Text=1, SystemNotification=2, StatusChange=3, InternalNote=4, MediaAttachment=5, Location=6). The composer maps them
/// to the SR <c>ServiceRequestMessageSenderType</c> / <c>ServiceRequestMessageType</c>.
/// </summary>
public sealed class SrTranscriptMessageDto
{
    public long MessageId { get; set; }
    public long SenderUserId { get; set; }
    public int SenderRole { get; set; }
    public int MessageType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentFileStorageId { get; set; }
    public decimal? LocationLat { get; set; }
    public decimal? LocationLng { get; set; }
    public string? LocationLabel { get; set; }
    public DateTimeOffset SentAt { get; set; }
}
