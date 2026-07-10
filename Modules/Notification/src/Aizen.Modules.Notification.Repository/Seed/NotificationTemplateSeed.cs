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
    ];
}
