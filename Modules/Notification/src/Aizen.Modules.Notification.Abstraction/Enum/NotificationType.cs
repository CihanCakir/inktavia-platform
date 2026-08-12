namespace Aizen.Modules.Notification.Abstraction.Enum;

public enum NotificationType
{
    ServiceRequestCreated        = 100,
    ServiceRequestStatusChanged  = 101,
    /// <summary>N-C — "Bölgende yeni iş talebi": a new service request opened in a provider's operating city/category.
    /// ServiceRequests category (100–132) so it inherits N-B ServiceRequests gating.</summary>
    ServiceRequestAreaOpportunity = 102,
    /// <summary>N2 — "{vessel} için {kategori} bakımı {tarih} tarihinde — planlayın": a recurring maintenance schedule
    /// (S12) is due soon; nudges the owner. ServiceRequests category (100–139) so it inherits N-B ServiceRequests gating.</summary>
    MaintenanceReminderDue       = 103,
    /// <summary>BE_NF1 (D1) — "Talebiniz yayında": the owner's service request is now published and open for offers.
    /// Supersedes the never-fired ServiceRequestCreated confirmation. Owner-facing. ServiceRequests category.</summary>
    ServiceRequestPublished      = 104,
    OfferCreated                 = 110,
    OfferAccepted                = 111,
    OfferRejected                = 112,
    /// <summary>BE_NF1 (D2) — "Yeni teklif aldınız": a provider submitted an offer on the owner's service request.
    /// Owner-facing counterpart to the provider's OfferCreated confirmation. ServiceRequests category.</summary>
    OfferReceived                = 113,
    AssignmentCreated            = 120,
    AssignmentAccepted           = 121,
    AssignmentRejected           = 122,
    /// <summary>BE_NF3 — "Sağlayıcı işinize başladı": the provider started the assigned job. Owner-facing.
    /// ServiceRequests category (100–139).</summary>
    JobStarted                   = 123,
    CompletionSubmitted          = 130,
    CompletionApproved           = 131,
    CompletionRejected           = 132,
    /// <summary>N3-C — "İş {n} gün içinde otomatik onaylanacak": the completion auto-approval deadline is approaching;
    /// nudges the owner to review. ServiceRequests category (100–139) so it inherits N-B ServiceRequests gating.</summary>
    CompletionAutoApproveApproaching = 133,
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
    /// <summary>N3-B — a gateway chargeback was recorded against a provider's transaction (P10 clawback / negative-balance
    /// impact). Notifies the provider + admins. Payments category (150–159, N-B gated).</summary>
    ChargebackRecorded           = 159,
    /// <summary>N1 (§13.2) — "aboneliğiniz {tarih} tarihinde ₺{yeni} olacak": an auto-renewing provider subscription's
    /// renewal price changes soon. Payments category (150–163, N-B gated). → provider.</summary>
    SubscriptionPriceChangeUpcoming = 160,
    /// <summary>N4 (§9) — "Teklif öne çıkarma aktif — {tarih}'e kadar": a premium boost entitlement was activated.
    /// Payments category (N-B gated). → provider.</summary>
    PremiumBoostActivated        = 161,
    /// <summary>N4 (§9) — "Öne çıkarma iptal edildi": a premium boost entitlement was revoked (refund). → provider.</summary>
    PremiumBoostRevoked          = 162,
    /// <summary>N4 (§19.7) — "{plan} kampanya bütçesi %{x} kaldı": a customer-benefit budget crossed the low threshold /
    /// exhausted; ops must top up before customer discounts silently stop. Payments category (N-B gated). → admins.</summary>
    BenefitBudgetLow             = 163,
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
    /// <summary>N-D — a live-support request was opened; notifies admins. Broadcast category (N-B gated).</summary>
    SupportRequestOpened         = 910,
}
