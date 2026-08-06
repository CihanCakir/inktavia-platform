using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>
/// BE-S13c — published when a dispute is resolved (in addition to the realtime <c>DisputeResolved</c> push). Carries
/// both parties + the monetary outcome so <b>N3</b> can notify owner and provider. <see cref="Outcome"/> is 0 for a
/// notes-only resolve (no money moved), else the <c>DisputeResolutionOutcome</c> int code; <see cref="RefundAmount"/>
/// is the amount refunded to the payer (0 for release / notes-only).
/// </summary>
[DocumentationInfo("Service request dispute resolved message", "Published when a dispute is resolved (BE-S13c).")]
public sealed class ServiceRequestDisputeResolvedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long DisputeId { get; set; }
    public long ResolvedByAdminUserId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderUserId { get; set; }

    /// <summary>The resolution reason the dispute was opened for (carried through for N3 context).</summary>
    public ServiceRequestDisputeReason Reason { get; set; }

    /// <summary>The <c>DisputeResolutionOutcome</c> int code; 0 for a notes-only resolve.</summary>
    public int Outcome { get; set; }

    /// <summary>Amount refunded to the payer (0 for provider-release / notes-only).</summary>
    public decimal RefundAmount { get; set; }
}
