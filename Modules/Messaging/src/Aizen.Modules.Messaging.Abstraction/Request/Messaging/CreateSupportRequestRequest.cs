using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Abstraction.Request.Messaging;

/// <summary>
/// N-D — open a live-support request. The requester identity is resolved server-side from the auth context;
/// only the topic/subject/first-message (and BFF-provided display name/role) come from the body.
/// </summary>
public sealed record CreateSupportRequestRequest(
    SupportTopic Topic,
    string Subject,
    string? FirstMessage = null,
    string? RequesterDisplayName = null,
    MessagingParticipantRole? RequesterRole = null
);
