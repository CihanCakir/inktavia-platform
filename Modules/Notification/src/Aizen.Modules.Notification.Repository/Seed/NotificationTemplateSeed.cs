using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Repository.Seed;

public sealed class NotificationTemplateSeed
{
    private readonly NotificationDbContext _db;
    private readonly ILogger<NotificationTemplateSeed> _logger;

    public NotificationTemplateSeed(NotificationDbContext db, ILogger<NotificationTemplateSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        foreach (var tpl in BuildTemplates())
        {
            var exists = await _db.NotificationTemplates.AnyAsync(x => x.TemplateCode == tpl.TemplateCode, ct);
            if (!exists)
            {
                await _db.NotificationTemplates.AddAsync(tpl, ct);
                _logger.LogInformation("Seeding notification template: {Code}", tpl.TemplateCode);
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private static List<NotificationTemplateEntity> BuildTemplates() =>
    [
        NotificationTemplateEntity.Create("SR_CREATED_INAPP", "Service Request Created (In-App)",
            NotificationType.ServiceRequestCreated, NotificationChannel.InApp,
            "New Service Request: {{requestCode}}",
            "A new service request {{requestCode}} has been created for {{serviceName}}."),

        // M4 — anonymous website contact received → admin fan-out.
        NotificationTemplateEntity.Create("CONTACT_RECEIVED_INAPP", "Contact Message Received (In-App)",
            NotificationType.ContactReceived, NotificationChannel.InApp,
            "New contact message: {{subject}}",
            "New website contact from {{name}} — \"{{subject}}\" (ref {{ticketRef}})."),

        // N-C region fan-out — provider-facing "new job in your area".
        NotificationTemplateEntity.Create("SR_AREA_OPPORTUNITY_INAPP", "Service Request in Your Area (In-App)",
            NotificationType.ServiceRequestAreaOpportunity, NotificationChannel.InApp,
            "Bölgende yeni iş talebi",
            "Bölgende yeni iş talebi: {{title}} ({{requestCode}})."),

        // BE_NF1 (D1) — owner-facing "your request is now live". Supersedes the never-fired SR_CREATED confirmation;
        // fires from the ServiceRequestPublished consumer (the event that actually publishes).
        NotificationTemplateEntity.Create("SR_PUBLISHED_INAPP", "Service Request Published (In-App)",
            NotificationType.ServiceRequestPublished, NotificationChannel.InApp,
            "Talebiniz yayında: {{requestCode}}",
            "Talebiniz {{requestCode}} artık yayında ve tekliflere açık."),

        // N-D live support — admin-facing "new support request".
        NotificationTemplateEntity.Create("SUPPORT_REQUEST_OPENED_INAPP", "Support Request Opened (In-App)",
            NotificationType.SupportRequestOpened, NotificationChannel.InApp,
            "Yeni destek talebi: {{topic}}",
            "{{requesterName}} yeni bir destek talebi açtı ({{topic}}): {{subject}}."),

        // Content module — admin-facing "new content published" awareness (from ContentPublishedConsumer).
        NotificationTemplateEntity.Create("CONTENT_PUBLISHED_INAPP", "Content Published (In-App)",
            NotificationType.ContentPublished, NotificationChannel.InApp,
            "Yeni içerik yayında",
            "{{contentType}} yayınlandı ({{slug}})."),

        NotificationTemplateEntity.Create("SR_STATUS_CHANGED_INAPP", "Service Request Status Changed (In-App)",
            NotificationType.ServiceRequestStatusChanged, NotificationChannel.InApp,
            "Request {{requestCode}} Status Updated",
            "Your request {{requestCode}} moved from {{fromStatus}} to {{toStatus}}."),

        NotificationTemplateEntity.Create("SR_OFFER_CREATED_INAPP", "Offer Received (In-App)",
            NotificationType.OfferCreated, NotificationChannel.InApp,
            "New Offer on Request #{{serviceRequestId}}",
            "A provider submitted a new offer for your request #{{serviceRequestId}}."),

        // BE_NF1 (D2) — owner-facing "you received an offer". Distinct from the provider's OfferCreated confirmation so
        // the owner (not just the provider) is notified when an offer lands. Without this template the type no-ops.
        NotificationTemplateEntity.Create("SR_OFFER_RECEIVED_INAPP", "Offer Received by Owner (In-App)",
            NotificationType.OfferReceived, NotificationChannel.InApp,
            "Yeni teklif aldınız — Talep #{{serviceRequestId}}",
            "Talebiniz #{{serviceRequestId}} için yeni bir teklif aldınız ({{totalAmount}} {{currencyCode}})."),

        // ── BE_NF2 — Email templates (tr) for the SR/offer/lifecycle types (mirror the InApp variables). Without a
        // seeded (Type, Email) template the Email send is a silent no-op. Email is N-B opt-in (default off). ──────────
        NotificationTemplateEntity.Create("SR_PUBLISHED_EMAIL", "Service Request Published (Email)",
            NotificationType.ServiceRequestPublished, NotificationChannel.Email,
            "Talebiniz yayında: {{requestCode}}",
            "Merhaba,\n\nTalebiniz {{requestCode}} artık yayında ve tekliflere açık. Gelen teklifleri uygulamadan " +
            "inceleyebilirsiniz.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_AREA_OPPORTUNITY_EMAIL", "Service Request in Your Area (Email)",
            NotificationType.ServiceRequestAreaOpportunity, NotificationChannel.Email,
            "Bölgende yeni iş talebi: {{requestCode}}",
            "Merhaba,\n\nBölgende yeni bir iş talebi açıldı: {{title}} ({{requestCode}}). Teklif vermek için " +
            "uygulamaya giriş yapın.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_OFFER_CREATED_EMAIL", "Offer Submitted (Email)",
            NotificationType.OfferCreated, NotificationChannel.Email,
            "Teklifiniz iletildi — Talep #{{serviceRequestId}}",
            "Merhaba,\n\n#{{serviceRequestId}} numaralı talep için {{totalAmount}} {{currencyCode}} tutarında " +
            "teklifiniz iletildi.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_OFFER_RECEIVED_EMAIL", "Offer Received by Owner (Email)",
            NotificationType.OfferReceived, NotificationChannel.Email,
            "Yeni teklif aldınız — Talep #{{serviceRequestId}}",
            "Merhaba,\n\nTalebiniz #{{serviceRequestId}} için yeni bir teklif aldınız ({{totalAmount}} " +
            "{{currencyCode}}). Teklifi incelemek için uygulamaya giriş yapın.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_ASSIGNMENT_CREATED_EMAIL", "Assignment Created (Email)",
            NotificationType.AssignmentCreated, NotificationChannel.Email,
            "Bir işe atandınız — Talep #{{serviceRequestId}}",
            "Merhaba,\n\n#{{serviceRequestId}} numaralı servis talebine atandınız. Lütfen uygulamadan inceleyip " +
            "onaylayın.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_COMPLETION_APPROVED_EMAIL", "Completion Approved (Email)",
            NotificationType.CompletionApproved, NotificationChannel.Email,
            "İş onaylandı — Talep #{{serviceRequestId}}",
            "Merhaba,\n\n#{{serviceRequestId}} numaralı talep için tamamlama onaylandı. Ödemeniz kısa süre içinde " +
            "serbest bırakılacaktır.\n\nInktavia Marine"),

        // ── BE_NF3 — lifecycle parity templates: JOB_STARTED (new, owner) + the missing (Type, Email) pairs for the
        // events that now go multi-channel (OfferAccepted→provider, CompletionSubmitted/AutoApprove/Maintenance→owner). ─
        NotificationTemplateEntity.Create("SR_JOB_STARTED_INAPP", "Job Started (In-App)",
            NotificationType.JobStarted, NotificationChannel.InApp,
            "İşiniz başladı — Talep #{{serviceRequestId}}",
            "Sağlayıcı, {{requestCode}} numaralı talebiniz için işe başladı."),

        NotificationTemplateEntity.Create("SR_JOB_STARTED_EMAIL", "Job Started (Email)",
            NotificationType.JobStarted, NotificationChannel.Email,
            "İşiniz başladı — Talep #{{serviceRequestId}}",
            "Merhaba,\n\nSağlayıcı, {{requestCode}} numaralı talebiniz için işe başladı. İlerlemeyi uygulamadan " +
            "takip edebilirsiniz.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_OFFER_ACCEPTED_EMAIL", "Offer Accepted (Email)",
            NotificationType.OfferAccepted, NotificationChannel.Email,
            "Teklifiniz kabul edildi — Talep #{{serviceRequestId}}",
            "Merhaba,\n\n#{{serviceRequestId}} numaralı talep için teklifiniz kabul edildi. Sonraki adımlar için " +
            "uygulamaya giriş yapın.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_COMPLETION_SUBMITTED_EMAIL", "Completion Submitted (Email)",
            NotificationType.CompletionSubmitted, NotificationChannel.Email,
            "Tamamlama gönderildi — Talep #{{serviceRequestId}}",
            "Merhaba,\n\nSağlayıcı, #{{serviceRequestId}} numaralı talep için işi tamamladığını bildirdi. Lütfen " +
            "inceleyip onaylayın.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_COMPLETION_AUTOAPPROVE_APPROACHING_EMAIL", "Completion Auto-Approve Approaching (Email)",
            NotificationType.CompletionAutoApproveApproaching, NotificationChannel.Email,
            "İş otomatik onaylanmak üzere — Talep #{{serviceRequestId}}",
            "Merhaba,\n\n#{{serviceRequestId}} numaralı talep için tamamlama {{daysRemaining}} gün içinde otomatik " +
            "onaylanacak. Lütfen inceleyin.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_MAINTENANCE_REMINDER_DUE_EMAIL", "Maintenance Reminder Due (Email)",
            NotificationType.MaintenanceReminderDue, NotificationChannel.Email,
            "Bakım hatırlatması — {{vessel}}",
            "Merhaba,\n\n{{vessel}} için {{category}} bakımı {{date}} tarihinde planlanmalı. Lütfen bir servis " +
            "talebi oluşturun.\n\nInktavia Marine"),

        NotificationTemplateEntity.Create("SR_OFFER_ACCEPTED_INAPP", "Offer Accepted (In-App)",
            NotificationType.OfferAccepted, NotificationChannel.InApp,
            "Your Offer Was Accepted",
            "Your offer on request #{{serviceRequestId}} has been accepted."),

        NotificationTemplateEntity.Create("SR_ASSIGNMENT_CREATED_INAPP", "Assignment Created (In-App)",
            NotificationType.AssignmentCreated, NotificationChannel.InApp,
            "You've Been Assigned to a Request",
            "You have been assigned to service request #{{serviceRequestId}}. Please review and confirm."),

        NotificationTemplateEntity.Create("SR_COMPLETION_SUBMITTED_INAPP", "Completion Submitted (In-App)",
            NotificationType.CompletionSubmitted, NotificationChannel.InApp,
            "Completion Submitted for Request #{{serviceRequestId}}",
            "The provider has submitted completion for request #{{serviceRequestId}}. Please review."),

        NotificationTemplateEntity.Create("SR_DISPUTE_OPENED_INAPP", "Dispute Opened (In-App)",
            NotificationType.DisputeOpened, NotificationChannel.InApp,
            "Dispute Opened for Request #{{serviceRequestId}}",
            "A dispute has been opened for service request #{{serviceRequestId}}."),

        // N3-A — dispute resolved (both parties). Outcome + amount rendered from the S13 resolution.
        NotificationTemplateEntity.Create("SR_DISPUTE_RESOLVED_INAPP", "Dispute Resolved (In-App)",
            NotificationType.DisputeResolved, NotificationChannel.InApp,
            "İtiraz çözüldü — Talep #{{serviceRequestId}}",
            "Talep #{{serviceRequestId}} için itiraz çözüldü: {{outcome}}{{refundSuffix}}."),

        // N3-C — completion approved (provider-facing; fires on manual + auto approval).
        NotificationTemplateEntity.Create("SR_COMPLETION_APPROVED_INAPP", "Completion Approved (In-App)",
            NotificationType.CompletionApproved, NotificationChannel.InApp,
            "İş onaylandı — Talep #{{serviceRequestId}}",
            "Talep #{{serviceRequestId}} için tamamlama onaylandı. Ödemeniz kısa süre içinde serbest bırakılacaktır."),

        // N3-C — auto-approve approaching reminder (owner-facing).
        NotificationTemplateEntity.Create("SR_COMPLETION_AUTOAPPROVE_APPROACHING_INAPP", "Completion Auto-Approve Approaching (In-App)",
            NotificationType.CompletionAutoApproveApproaching, NotificationChannel.InApp,
            "İş otomatik onaylanmak üzere — Talep #{{serviceRequestId}}",
            "Talep #{{serviceRequestId}} için tamamlama {{daysRemaining}} gün içinde otomatik onaylanacak. Lütfen inceleyin."),

        // N2 — recurring maintenance due reminder (owner-facing). Without this template the type is a silent no-op.
        NotificationTemplateEntity.Create("SR_MAINTENANCE_REMINDER_DUE_INAPP", "Maintenance Reminder Due (In-App)",
            NotificationType.MaintenanceReminderDue, NotificationChannel.InApp,
            "Bakım hatırlatması — {{vessel}}",
            "{{vessel}} için {{category}} bakımı {{date}} tarihinde planlanmalı. Lütfen bir servis talebi oluşturun."),

        NotificationTemplateEntity.Create("SR_PAYMENT_RELEASED_INAPP", "Payment Released (In-App)",
            NotificationType.PaymentReleased, NotificationChannel.InApp,
            "Payment Released: Request #{{serviceRequestId}}",
            "Payment for service request #{{serviceRequestId}} has been released."),

        // ── BE-MO9c — owner event-set gap-fill: types that had a producer/consumer path but NO template were a
        // silent no-op. These four complete the owner set (duplicate-safe by TemplateCode). Placeholder keys match
        // the emitting consumers' Variables (PaymentCaptured: amount/currency/transactionCode;
        // PaymentRefunded: refundedAmount/currency/transactionCode; offer/completion: serviceRequestId).
        NotificationTemplateEntity.Create("SR_OFFER_REJECTED_INAPP", "Offer Rejected (In-App)",
            NotificationType.OfferRejected, NotificationChannel.InApp,
            "Teklif reddedildi — Talep #{{serviceRequestId}}",
            "Talep #{{serviceRequestId}} için bir teklif reddedildi."),

        NotificationTemplateEntity.Create("SR_COMPLETION_REJECTED_INAPP", "Completion Rejected (In-App)",
            NotificationType.CompletionRejected, NotificationChannel.InApp,
            "İş reddedildi — Talep #{{serviceRequestId}}",
            "Talep #{{serviceRequestId}} için tamamlama reddedildi. Lütfen sağlayıcıyla iletişime geçin."),

        NotificationTemplateEntity.Create("PAYMENT_CAPTURED_INAPP", "Payment Captured (In-App)",
            NotificationType.PaymentCaptured, NotificationChannel.InApp,
            "Ödeme alındı",
            "{{amount}} {{currency}} tutarında ödeme alındı (işlem {{transactionCode}})."),

        NotificationTemplateEntity.Create("PAYMENT_REFUNDED_INAPP", "Payment Refunded (In-App)",
            NotificationType.PaymentRefunded, NotificationChannel.InApp,
            "Ödeme iadesi",
            "{{refundedAmount}} {{currency}} tutarında iade yapıldı (işlem {{transactionCode}})."),

        // N-C latent (Payment BE-P9 PreAuth) — template ready so PaymentAuthorizedConsumer renders once P9 fires it.
        NotificationTemplateEntity.Create("PAYMENT_AUTHORIZED_INAPP", "Payment Authorized / PreAuth Hold (In-App)",
            NotificationType.PaymentAuthorized, NotificationChannel.InApp,
            "Ödeme Provizyonda",
            "{{amount}} {{currency}} tutarında provizyon alındı (işlem {{transactionCode}})."),

        // N3-B — chargeback recorded (provider + admin). Provider sees the P10 clawback / negative-balance impact.
        NotificationTemplateEntity.Create("PAYMENT_CHARGEBACK_RECORDED_INAPP", "Chargeback Recorded (In-App)",
            NotificationType.ChargebackRecorded, NotificationChannel.InApp,
            "Ters ibraz kaydedildi",
            "{{amount}} {{currency}} tutarında bir ters ibraz kaydedildi (işlem {{transactionCode}}, ref {{gatewayRef}})."),

        // N1 (§13.2) — subscription renewal price change (provider-facing).
        NotificationTemplateEntity.Create("SUB_PRICE_CHANGE_UPCOMING_INAPP", "Subscription Price Change Upcoming (In-App)",
            NotificationType.SubscriptionPriceChangeUpcoming, NotificationChannel.InApp,
            "Abonelik fiyatı değişiyor — {{plan}}",
            "{{plan}} aboneliğiniz {{date}} tarihinde {{newPrice}} {{currency}} olacak (şu an {{currentPrice}} {{currency}})."),

        // N4 (§9) — premium boost activated (provider-facing).
        NotificationTemplateEntity.Create("PREMIUM_BOOST_ACTIVATED_INAPP", "Premium Boost Activated (In-App)",
            NotificationType.PremiumBoostActivated, NotificationChannel.InApp,
            "Teklif öne çıkarma aktif",
            "Teklif #{{offerId}} öne çıkarma aktif — {{expiresAt}} tarihine kadar geçerli."),

        // N4 (§9) — premium boost revoked (provider-facing).
        NotificationTemplateEntity.Create("PREMIUM_BOOST_REVOKED_INAPP", "Premium Boost Revoked (In-App)",
            NotificationType.PremiumBoostRevoked, NotificationChannel.InApp,
            "Öne çıkarma iptal edildi",
            "Teklif #{{offerId}} için öne çıkarma iptal edildi."),

        // N4 (§19.7) — customer-benefit budget low/exhausted (admin-facing).
        NotificationTemplateEntity.Create("BENEFIT_BUDGET_LOW_INAPP", "Benefit Budget Low (In-App)",
            NotificationType.BenefitBudgetLow, NotificationChannel.InApp,
            "Kampanya bütçesi azaldı",
            "Plan #{{plan}} kampanya bütçesinde {{remaining}} {{currency}} kaldı (eşik %{{percent}}). Lütfen bütçeyi artırın."),

        NotificationTemplateEntity.Create("MSG_NEW_MESSAGE_INAPP", "New Message (In-App)",
            NotificationType.NewMessageReceived, NotificationChannel.InApp,
            "New message from {{senderName}}",
            "{{senderName}} sent a message in conversation {{conversationTitle}}."),

        NotificationTemplateEntity.Create("CD_KIT_EXPIRING_INAPP", "Kit Expiring Reminder (In-App)",
            NotificationType.CargoDryKitExpiringReminder, NotificationChannel.InApp,
            "CargoDry Kit Expiring Soon",
            "Your CargoDry kit {{kitCode}} expires in {{daysLeft}} days. Please renew."),

        NotificationTemplateEntity.Create("CD_KIT_ACTIVATED_INAPP", "Kit Activated (In-App)",
            NotificationType.CargoDryKitActivated, NotificationChannel.InApp,
            "CargoDry Kit Activated",
            "Your CargoDry kit {{kitCode}} ({{productName}}) has been successfully activated. Valid until {{expiryDate}}."),

        NotificationTemplateEntity.Create("CD_KIT_EXPIRED_INAPP", "Kit Expired (In-App)",
            NotificationType.CargoDryKitExpired, NotificationChannel.InApp,
            "CargoDry Kit Expired",
            "Your CargoDry kit {{kitCode}} has expired."),

        NotificationTemplateEntity.Create("CD_KIT_RENEWED_INAPP", "Kit Renewed (In-App)",
            NotificationType.CargoDryKitRenewed, NotificationChannel.InApp,
            "CargoDry Kit Renewed",
            "Kit {{kitCode}} renewed ({{renewalType}}). New expiry: {{newExpiryDate}}."),

        NotificationTemplateEntity.Create("CD_KIT_REVOKED_INAPP", "Kit Revoked (In-App)",
            NotificationType.CargoDryKitRevoked, NotificationChannel.InApp,
            "CargoDry Kit Revoked",
            "Kit {{kitCode}} has been revoked. Reason: {{reason}}."),

        // ── Phase 11: Renewal notification templates ──────────────────────────
        NotificationTemplateEntity.Create("CD_RENEWAL_NOTIFICATION_INAPP", "Kit Renewal Notification (In-App)",
            NotificationType.CargoDryRenewalNotificationRequested, NotificationChannel.InApp,
            "CargoDry Kit Renewal Available",
            "Your CargoDry kit {{kitCode}} ({{productName}}) expires in {{daysUntilExpiry}} days. " +
            "Renewal price: {{renewalPrice}}. Contact your marina to renew. Ref: {{renewalCode}}."),

        NotificationTemplateEntity.Create("CD_RENEWAL_NOTIFICATION_EMAIL", "Kit Renewal Notification (Email)",
            NotificationType.CargoDryRenewalNotificationRequested, NotificationChannel.Email,
            "CargoDry Kit Renewal Notice — {{kitCode}}",
            "Dear customer,\n\n" +
            "Your CargoDry kit {{kitCode}} ({{productName}}) will expire in {{daysUntilExpiry}} days " +
            "({{expiryDate}}).\n\n" +
            "Renewal price: {{renewalPrice}}\n" +
            "Reference: {{renewalCode}}\n\n" +
            "Please contact your service provider to complete the renewal.\n\n" +
            "Inktavia Marine Platform"),

        NotificationTemplateEntity.Create("CD_RENEWAL_NOTIFICATION_SMS", "Kit Renewal Notification (SMS)",
            NotificationType.CargoDryRenewalNotificationRequested, NotificationChannel.Sms,
            "CargoDry Renewal",
            "Your CargoDry kit {{kitCode}} expires in {{daysUntilExpiry}} days. " +
            "Price: {{renewalPrice}}. Ref: {{renewalCode}}. Contact your marina to renew."),

        NotificationTemplateEntity.Create("PROFILE_APPROVAL_DECISION_INAPP", "Profile Approval Decision (In-App)",
            NotificationType.ProfileApprovalDecision, NotificationChannel.InApp,
            "Profile Review Complete",
            "Your profile has been {{decision}}. {{reason}}"),

        // ── Provider Lifecycle Events ──────────────────────────────────────────

        // Approved
        NotificationTemplateEntity.Create("PROFILE_APPROVED_INAPP", "Profile Approved (In-App)",
            NotificationType.ProfileApproved, NotificationChannel.InApp,
            "Profiliniz onaylandı",
            "Profiliniz başarıyla onaylandı. Artık platformu kullanmaya başlayabilirsiniz."),

        NotificationTemplateEntity.Create("PROFILE_APPROVED_PUSH", "Profile Approved (Push)",
            NotificationType.ProfileApproved, NotificationChannel.Push,
            "Profiliniz onaylandı",
            "Profiliniz başarıyla onaylandı. Platforma giriş yapabilirsiniz."),

        // Rejected
        NotificationTemplateEntity.Create("PROFILE_REJECTED_INAPP", "Profile Rejected (In-App)",
            NotificationType.ProfileRejected, NotificationChannel.InApp,
            "Profiliniz reddedildi",
            "Profiliniz reddedildi. Sebep: {{reason}}"),

        NotificationTemplateEntity.Create("PROFILE_REJECTED_PUSH", "Profile Rejected (Push)",
            NotificationType.ProfileRejected, NotificationChannel.Push,
            "Profiliniz reddedildi",
            "Profiliniz reddedildi. Detaylar için uygulamaya giriş yapın."),

        // Suspended
        NotificationTemplateEntity.Create("PROFILE_SUSPENDED_INAPP", "Profile Suspended (In-App)",
            NotificationType.ProfileSuspended, NotificationChannel.InApp,
            "Profiliniz askıya alındı",
            "Profiliniz askıya alındı. Sebep: {{reason}}"),

        NotificationTemplateEntity.Create("PROFILE_SUSPENDED_PUSH", "Profile Suspended (Push)",
            NotificationType.ProfileSuspended, NotificationChannel.Push,
            "Profiliniz askıya alındı",
            "Profiliniz askıya alındı. Detaylar için uygulamaya giriş yapın."),

        // Revision Requested
        NotificationTemplateEntity.Create("ONBOARDING_REVISION_INAPP", "Onboarding Revision Requested (In-App)",
            NotificationType.OnboardingRevisionRequested, NotificationChannel.InApp,
            "Başvurunuzda düzenleme istendi",
            "Başvurunuzda düzenleme istendi. Adımlar: {{steps}}. Not: {{note}}"),

        NotificationTemplateEntity.Create("ONBOARDING_REVISION_PUSH", "Onboarding Revision Requested (Push)",
            NotificationType.OnboardingRevisionRequested, NotificationChannel.Push,
            "Başvurunuzda düzenleme istendi",
            "Başvurunuzun bazı adımlarında düzenleme yapmanız gerekmektedir."),

        // ── Password Recovery OTP ───────────────────────────────────────────────
        NotificationTemplateEntity.Create("PWD_RECOVERY_OTP_EMAIL", "Password Recovery OTP (Email)",
            NotificationType.PasswordRecoveryOtp, NotificationChannel.Email,
            "Inktavia — Password reset code",
            "Your password reset code is {{otp}}. It expires in {{expiresMinutes}} minutes.\n\n" +
            "If you did not request this, ignore this email."),

        NotificationTemplateEntity.Create("PWD_RECOVERY_OTP_SMS", "Password Recovery OTP (SMS)",
            NotificationType.PasswordRecoveryOtp, NotificationChannel.Sms,
            "Inktavia Password Reset",
            "Your Inktavia password reset code is {{otp}}. Expires in {{expiresMinutes}} min."),

        // ── OTP Login ───────────────────────────────────────────────────────────
        NotificationTemplateEntity.Create("OTP_LOGIN_EMAIL", "OTP Login Code (Email)",
            NotificationType.OtpLoginCode, NotificationChannel.Email,
            "Inktavia — Login verification code",
            "Your login verification code is {{otp}}. It expires in {{expiresMinutes}} minutes.\n\n" +
            "If you did not request this, ignore this email."),

        NotificationTemplateEntity.Create("OTP_LOGIN_SMS", "OTP Login Code (SMS)",
            NotificationType.OtpLoginCode, NotificationChannel.Sms,
            "Inktavia Login Code",
            "Your Inktavia login code is {{otp}}. Expires in {{expiresMinutes}} min."),

        // ── CargoDry Provider Milestone Notifications (CE-6c) ──────────────────
        NotificationTemplateEntity.Create("CD_FIRST_SALE_INAPP", "CargoDry First Sale (In-App)",
            NotificationType.CargoDryProviderFirstSale, NotificationChannel.InApp,
            "Congratulations on your first sale!",
            "Your first sale has been completed! Your CargoDry commission earnings have started."),

        NotificationTemplateEntity.Create("CD_MONTHLY_TARGET_INAPP", "CargoDry Monthly Target Reached (In-App)",
            NotificationType.CargoDryProviderMonthlyTargetReached, NotificationChannel.InApp,
            "Monthly target reached!",
            "You've hit your monthly target! You reached {{displayValue}} in commission."),

        NotificationTemplateEntity.Create("CD_TIER_UP_INAPP", "CargoDry Tier Up (In-App)",
            NotificationType.CargoDryProviderTierUp, NotificationChannel.InApp,
            "Tier upgrade!",
            "You've been promoted to the {{displayValue}} tier!"),

        NotificationTemplateEntity.Create("CD_STREAK_INAPP", "CargoDry Streak Milestone (In-App)",
            NotificationType.CargoDryProviderStreakMilestone, NotificationChannel.InApp,
            "Sales streak milestone!",
            "{{displayValue}} consecutive months of sales! Your momentum is going strong."),
    ];
}
