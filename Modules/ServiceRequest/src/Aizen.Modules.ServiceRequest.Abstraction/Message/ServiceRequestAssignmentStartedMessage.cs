using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>
/// BE_WC1 — published when a provider starts an assignment (job). A first-class lifecycle event so the Messaging module
/// can generate the <c>JOB_STARTED</c> System message independently of the chat-mirror event (which WC4 removes).
/// Additive: published ALONGSIDE the existing WorkStarted realtime + the chat event; nothing is removed.
/// </summary>
[DocumentationInfo("Service request assignment started message",
    "Published when a provider starts the assignment (job) — drives the JOB_STARTED System message in Messaging.")]
public sealed class ServiceRequestAssignmentStartedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public long AssignmentId { get; set; }
    public long ProviderProfileId { get; set; }
    /// <summary>The provider user that started the job (the System-message actor / audit).</summary>
    public long StartedByUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
