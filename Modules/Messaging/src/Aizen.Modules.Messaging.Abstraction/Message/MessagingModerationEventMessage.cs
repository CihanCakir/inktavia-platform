using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Messaging.Abstraction.Message;

/// <summary>
/// Published by the Messaging module on an admin-relevant moderation change (flag / moderate / status change).
/// Consumed by the AdminPanel BFF realtime edge, which pushes a thin "moderationEvent" frame to admin sessions so
/// the moderation queue + affected conversation refetch LIVE (instead of waiting for the 30s poll). Mirrors how
/// <see cref="MessagingMessageSentMessage"/> feeds the message live-feed. Intentionally THIN — ids + kind only,
/// NO message content / PII.
/// </summary>
public sealed class MessagingModerationEventMessage : AizenBaseMessage
{
    public long           ConversationId { get; set; }
    /// <summary>The affected message, when the change targets a single message (moderate); null for conversation-level flag.</summary>
    public long?          MessageId      { get; set; }
    /// <summary>Flagged | Moderated | StatusChanged.</summary>
    public string         Kind           { get; set; } = default!;
    /// <summary>New moderation/conversation status (e.g. "Blocked", "Flagged"), when applicable.</summary>
    public string?        NewStatus      { get; set; }
    public DateTimeOffset OccurredAt     { get; set; }
}
