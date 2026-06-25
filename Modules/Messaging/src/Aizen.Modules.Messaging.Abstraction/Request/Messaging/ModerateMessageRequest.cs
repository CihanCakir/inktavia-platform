using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Request.Messaging;

[DocumentationInfo("Moderate message request", "Payload for updating moderation verdict on a message.")]
public sealed record ModerateMessageRequest(
    MessageModerationStatus Status,
    string? Reason = null
);
