namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

/// <summary>
/// BE_WC3b — the ordered chat transcript for a conversation resolved by domain context (e.g. a ServiceRequest). Returned
/// by the internal (service-to-service) transcript endpoint and consumed by the ServiceRequest module to compose the
/// dispute case transcript from the canonical Messaging store instead of <c>sr.Messages</c>.
/// </summary>
public sealed class ConversationTranscriptResponse
{
    public IReadOnlyList<TranscriptMessageDto> Messages { get; init; } = new List<TranscriptMessageDto>();
}

/// <summary>
/// One message in the transcript. <see cref="SenderRole"/> and <see cref="MessageType"/> are the <b>integer values</b>
/// of the Messaging enums (<c>MessagingParticipantRole</c>: Owner=1, Provider=2, Admin=3, System=4, Support=5, Buyer=6,
/// Seller=7 · <c>MessageType</c>: Text=1, SystemNotification=2, StatusChange=3, InternalNote=4, MediaAttachment=5,
/// Location=6). They are exposed as <c>int</c> on purpose: the cross-module contract stays a stable numeric so a caller
/// that keeps its own local projections (the SR convention) never has to reference this module's enums, and there is no
/// string/number enum-serialization ambiguity across the module boundary.
/// </summary>
public sealed record TranscriptMessageDto
{
    public long MessageId { get; init; }
    public long SenderUserId { get; init; }
    public int SenderRole { get; init; }
    public int MessageType { get; init; }
    public string Content { get; init; } = string.Empty;

    /// <summary>The first attachment's <c>FileStorageId</c> (the fileId string) when present — else null.</summary>
    public string? AttachmentFileStorageId { get; init; }

    public decimal? LocationLat { get; init; }
    public decimal? LocationLng { get; init; }
    public string? LocationLabel { get; init; }

    public DateTimeOffset SentAt { get; init; }
}
