namespace Aizen.Modules.Messaging.Abstraction.Enum;

/// <summary>Content moderation verdict for a message.</summary>
public enum MessageModerationStatus
{
    Allowed        = 1,
    PendingReview  = 2,
    Flagged        = 3,
    Blocked        = 4,
    AutoApproved   = 5,
}
