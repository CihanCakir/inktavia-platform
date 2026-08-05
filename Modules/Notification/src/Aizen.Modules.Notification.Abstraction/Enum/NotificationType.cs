namespace Aizen.Modules.Notification.Abstraction.Enum;

public enum NotificationType
{
    ServiceRequestCreated        = 100,
    ServiceRequestStatusChanged  = 101,
    /// <summary>N-C — "Bölgende yeni iş talebi": a new service request opened in a provider's operating city/category.
    /// ServiceRequests category (100–132) so it inherits N-B ServiceRequests gating.</summary>
    ServiceRequestAreaOpportunity = 102,
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
    PaymentCaptured              = 151,
    PaymentCancelled             = 152,
    PaymentRefunded              = 153,
    PaymentReminderDue           = 154,
    PayoutCompleted              = 155,
    PaymentFailed                = 156,
    /// <summary>"Ödeme Provizyonda" — a PreAuth/authorization hold was placed (payment authorized, not yet captured).
    /// Latent: fires only once Payment BE-P9 PreAuth mode publishes a PaymentAuthorizedMessage. Payments category.</summary>
    PaymentAuthorized            = 157,
    NewMessageReceived           = 200,
    MessageBlocked               = 201,
    CargoDryKitActivated         = 300,
    CargoDryKitExpiringReminder  = 301,
    CargoDryKitExpired           = 302,
    CargoDryKitRenewed           = 303,
    CargoDryKitRevoked                     = 304,
    /// <summary>Admin-triggered renewal notification dispatched through the Notification module. Phase 11.</summary>
    CargoDryRenewalNotificationRequested   = 305,
    CargoDryProviderFirstSale              = 306,
    CargoDryProviderMonthlyTargetReached   = 307,
    CargoDryProviderTierUp                 = 308,
    CargoDryProviderStreakMilestone         = 309,
    ProfileApprovalDecision      = 400,
    ProfileApproved              = 401,
    ProfileRejected              = 402,
    ProfileSuspended             = 403,
    OnboardingRevisionRequested  = 404,
    PasswordRecoveryOtp          = 410,
    OtpLoginCode                 = 411,
    AdminBroadcast               = 900,
}
