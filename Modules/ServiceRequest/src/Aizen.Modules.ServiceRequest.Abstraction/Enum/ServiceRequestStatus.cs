
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest status enum", "Lifecycle status values for a service request from creation to closure.")]
public enum ServiceRequestStatus
{
    Draft = 1,
    Open = 10,
    WaitingForOffer = 11,
    OfferReceived = 12,
    OfferAccepted = 13,
    OfferRejected = 14,
    WaitingForAssignment = 20,
    Assigned = 21,
    Scheduled = 22,
    InProgress = 30,
    WaitingForOwnerApproval = 31,
    WaitingForMaterial = 32,
    Paused = 33,
    CompletionSubmitted = 40,
    Completed = 41,
    DisputeOpened = 50,
    UnderDisputeReview = 51,
    DisputeResolved = 52,
    Cancelled = 90,
    Expired = 91,
    Closed = 99
}
