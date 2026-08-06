using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>
/// N3-C — emitted by the completion auto-approval job when a still-pending completion is within the reminder-lead
/// window of its <c>AutoApproveAt</c> deadline. The Notification module turns it into a
/// <c>CompletionAutoApproveApproaching</c> (133) nudge to the owner. Emitted once per completion (guarded by
/// <c>AutoApproveReminderSentAt</c>).
/// </summary>
public sealed class ServiceRequestCompletionAutoApproveApproachingMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long CompletionId { get; set; }
    public long OwnerUserId { get; set; }
    public DateTime AutoApproveAtUtc { get; set; }
    public int DaysRemaining { get; set; }
}
