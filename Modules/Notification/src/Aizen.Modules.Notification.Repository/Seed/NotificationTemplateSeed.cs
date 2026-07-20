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

        NotificationTemplateEntity.Create("SR_STATUS_CHANGED_INAPP", "Service Request Status Changed (In-App)",
            NotificationType.ServiceRequestStatusChanged, NotificationChannel.InApp,
            "Request {{requestCode}} Status Updated",
            "Your request {{requestCode}} moved from {{fromStatus}} to {{toStatus}}."),

        NotificationTemplateEntity.Create("SR_OFFER_CREATED_INAPP", "Offer Received (In-App)",
            NotificationType.OfferCreated, NotificationChannel.InApp,
            "New Offer on Request #{{serviceRequestId}}",
            "A provider submitted a new offer for your request #{{serviceRequestId}}."),

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

        NotificationTemplateEntity.Create("SR_PAYMENT_RELEASED_INAPP", "Payment Released (In-App)",
            NotificationType.PaymentReleased, NotificationChannel.InApp,
            "Payment Released: Request #{{serviceRequestId}}",
            "Payment for service request #{{serviceRequestId}} has been released."),

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
