namespace Aizen.Modules.Notification.Abstraction.Enum;

public enum NotificationType
{
    ServiceRequestCreated        = 100,
    ServiceRequestStatusChanged  = 101,
    OfferCreated                 = 110,
    OfferAccepted                = 111,
    OfferRejected                = 112,
    AssignmentCreated            = 120,
    AssignmentAccepted           = 121,
    AssignmentRejected           = 122,
    CompletionSubmitted          = 130,
    CompletionApproved           = 131,
    CompletionRejected           = 132,
    DisputeOpened                = 140,
    DisputeResolved              = 141,
    PaymentReleased              = 150,
    NewMessageReceived           = 200,
    MessageBlocked               = 201,
    CargoDryKitActivated         = 300,
    CargoDryKitExpiringReminder  = 301,
    CargoDryKitExpired           = 302,
    ProfileApprovalDecision      = 400,
    AdminBroadcast               = 900,
}
