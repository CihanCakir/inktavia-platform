using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Timeline;

/// <summary>
/// Derives a STABLE, machine-readable timeline event code from the stored (FromStatus, ToStatus) of a status-history
/// row — so clients can localize the timeline instead of the English prose that lives in <c>Reason</c>.
///
/// Derived at READ time from the two structured columns, so historical rows get a code too (the prose was never
/// machine-readable). The event is keyed on the destination status, with one from-status split: a return to
/// InProgress from CompletionSubmitted is a completion REJECTION, not a work start. Unknown/arbitrary transitions
/// (admin free-status updates) fall back to <c>SR_STATUS_CHANGED</c>.
/// </summary>
public static class ServiceRequestTimelineEventCode
{
    /// <summary>
    /// The AUTHORITATIVE closed vocabulary every code <see cref="Derive"/> can emit. Clients localize against this
    /// set; a guard test asserts no status transition can produce a code outside it, so the vocabulary cannot grow
    /// silently — any addition forces a code change here (and a heads-up to the client teams).
    /// </summary>
    public static readonly IReadOnlySet<string> CanonicalCodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "SR_CREATED",
        "SR_PUBLISHED",
        "SR_WAITING_FOR_OFFER",
        "SR_OFFER_RECEIVED",
        "SR_OFFER_ACCEPTED",
        "SR_OFFER_REJECTED",
        "SR_WAITING_FOR_ASSIGNMENT",
        "SR_ASSIGNED",
        "SR_SCHEDULED",
        "SR_WORK_STARTED",
        "SR_COMPLETION_REJECTED",
        "SR_WAITING_OWNER_APPROVAL",
        "SR_WAITING_MATERIAL",
        "SR_PAUSED",
        "SR_COMPLETION_SUBMITTED",
        "SR_COMPLETED",
        "SR_DISPUTE_OPENED",
        "SR_UNDER_DISPUTE_REVIEW",
        "SR_DISPUTE_RESOLVED",
        "SR_CANCELLED",
        "SR_EXPIRED",
        "SR_CLOSED",
        "SR_STATUS_CHANGED",
    };

    public static string Derive(ServiceRequestStatus fromStatus, ServiceRequestStatus toStatus) => toStatus switch
    {
        ServiceRequestStatus.Draft => "SR_CREATED",
        ServiceRequestStatus.Open => "SR_PUBLISHED",
        ServiceRequestStatus.WaitingForOffer => "SR_WAITING_FOR_OFFER",
        ServiceRequestStatus.OfferReceived => "SR_OFFER_RECEIVED",
        ServiceRequestStatus.OfferAccepted => "SR_OFFER_ACCEPTED",
        ServiceRequestStatus.OfferRejected => "SR_OFFER_REJECTED",
        ServiceRequestStatus.WaitingForAssignment => "SR_WAITING_FOR_ASSIGNMENT",
        ServiceRequestStatus.Assigned => "SR_ASSIGNED",
        ServiceRequestStatus.Scheduled => "SR_SCHEDULED",
        ServiceRequestStatus.InProgress =>
            fromStatus == ServiceRequestStatus.CompletionSubmitted ? "SR_COMPLETION_REJECTED" : "SR_WORK_STARTED",
        ServiceRequestStatus.WaitingForOwnerApproval => "SR_WAITING_OWNER_APPROVAL",
        ServiceRequestStatus.WaitingForMaterial => "SR_WAITING_MATERIAL",
        ServiceRequestStatus.Paused => "SR_PAUSED",
        ServiceRequestStatus.CompletionSubmitted => "SR_COMPLETION_SUBMITTED",
        ServiceRequestStatus.Completed => "SR_COMPLETED",
        ServiceRequestStatus.DisputeOpened => "SR_DISPUTE_OPENED",
        ServiceRequestStatus.UnderDisputeReview => "SR_UNDER_DISPUTE_REVIEW",
        ServiceRequestStatus.DisputeResolved => "SR_DISPUTE_RESOLVED",
        ServiceRequestStatus.Cancelled => "SR_CANCELLED",
        ServiceRequestStatus.Expired => "SR_EXPIRED",
        ServiceRequestStatus.Closed => "SR_CLOSED",
        _ => "SR_STATUS_CHANGED",
    };
}
