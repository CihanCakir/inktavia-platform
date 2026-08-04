using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request message sent message", "Published when a participant sends a message.")]
public sealed class ServiceRequestMessageSentMessage : AizenBaseMessage
{
    // ── Existing fields (unchanged — provider realtime + any consumer keep working) ──
    public long ServiceRequestId { get; set; }
    public long MessageId { get; set; }
    public long SenderUserId { get; set; }
    public ServiceRequestMessageSenderType SenderType { get; set; }
    public long? ProviderProfileId { get; set; }

    // ── Additive enrichment (Phase-2 SR→Messaging live-sync) — all nullable; existing consumers ignore them ──
    /// <summary>Message body (or the machine code for System messages, e.g. "OFFER_ACCEPTED").</summary>
    public string? Content { get; set; }
    public ServiceRequestMessageType? MessageType { get; set; }
    public Guid? AttachmentFileId { get; set; }
    public decimal? LocationLat { get; set; }
    public decimal? LocationLng { get; set; }
    public string? LocationLabel { get; set; }
    /// <summary>Resolved sender display name if the publisher had it; null → the sync resolves it from UserProfiles.</summary>
    public string? SenderName { get; set; }
    /// <summary>Publish-time UTC (the message is not yet saved, so its persisted CreateDate is unavailable here).</summary>
    public DateTimeOffset? OccurredAt { get; set; }
}
