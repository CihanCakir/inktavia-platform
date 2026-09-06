using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// NotificationType → o tip için izin verilen {{placeholder}} adları. İçerik, tahminle değil, Consumers/* kaynak
/// kodundaki `variables` sözlüklerinin BİREBİR anahtarlarından türetilmiştir (her consumer'ın koyduğu anahtarlar).
/// Not: OTP/doğrulama consumer'larında recipientEmail/recipientPhone MetadataJson'a gider, template değişkeni DEĞİL —
/// bu yüzden buraya dahil edilmez (istisna: CargoDryRenewal onları doğrudan Variables'a koyar).
/// Bu bir referans kataloğudur (preview/doğrulama için tek doğru kaynak).
/// </summary>
public static class NotificationVariablesCatalog
{
    private static readonly IReadOnlyDictionary<NotificationType, IReadOnlyList<string>> _map =
        new Dictionary<NotificationType, IReadOnlyList<string>>
        {
            // ── ServiceRequest ─────────────────────────────────────────────
            [NotificationType.ServiceRequestPublished]           = new[] { "requestCode", "serviceName" },
            [NotificationType.ServiceRequestAreaOpportunity]     = new[] { "title", "requestCode" },
            [NotificationType.ServiceRequestStatusChanged]       = new[] { "requestCode", "fromStatus", "toStatus" },
            [NotificationType.MaintenanceReminderDue]            = new[] { "vessel", "category", "date" },
            [NotificationType.OfferCreated]                      = new[] { "serviceRequestId", "offerId", "totalAmount", "currencyCode" },
            [NotificationType.OfferReceived]                     = new[] { "serviceRequestId", "offerId", "totalAmount", "currencyCode" },
            [NotificationType.OfferAccepted]                     = new[] { "serviceRequestId", "offerId" },
            [NotificationType.AssignmentCreated]                 = new[] { "serviceRequestId", "assignmentId", "scheduledStartDate" },
            [NotificationType.JobStarted]                        = new[] { "serviceRequestId", "requestCode" },
            [NotificationType.TripStarted]                       = new[] { "serviceRequestId", "requestCode" },
            [NotificationType.CompletionSubmitted]               = new[] { "serviceRequestId", "completionId" },
            [NotificationType.CompletionApproved]                = new[] { "serviceRequestId", "completionId" },
            [NotificationType.CompletionAutoApproveApproaching]  = new[] { "serviceRequestId", "completionId", "daysRemaining" },
            [NotificationType.DisputeOpened]                     = new[] { "serviceRequestId", "disputeId", "reason" },
            [NotificationType.DisputeResolved]                   = new[] { "serviceRequestId", "disputeId", "outcome", "refundAmount", "refundSuffix" },

            // ── Payment ────────────────────────────────────────────────────
            [NotificationType.PaymentCaptured]                   = new[] { "transactionCode", "amount", "currency", "capturedAt" },
            [NotificationType.PaymentCancelled]                  = new[] { "transactionCode", "amount", "currency", "cancellationReason", "cancelledAt" },
            [NotificationType.PaymentAuthorized]                 = new[] { "transactionCode", "amount", "currency", "authorizedAt" },
            [NotificationType.PaymentReleased]                   = new[] { "transactionCode", "netPayoutAmount", "commissionAmount", "currency", "releasedAt" },
            [NotificationType.PaymentRefunded]                   = new[] { "transactionCode", "refundedAmount", "originalAmount", "currency", "isPartial", "reason", "refundedAt" },
            [NotificationType.PaymentReminderDue]                = new[] { "transactionCode", "amount", "currency", "reminderWindow", "pendingSinceUtc" },
            [NotificationType.PaymentFailed]                     = new[] { "transactionCode", "amount", "currency", "failureReason", "isRetryable", "failedAt" },
            [NotificationType.PayoutCompleted]                   = new[] { "amount", "currency", "gatewayProvider", "processedAt" },
            [NotificationType.ChargebackRecorded]                = new[] { "transactionCode", "amount", "currency", "gatewayRef" },
            [NotificationType.SubscriptionPriceChangeUpcoming]   = new[] { "plan", "currentPrice", "newPrice", "currency", "date" },
            [NotificationType.PremiumBoostActivated]             = new[] { "offerId", "expiresAt" },
            [NotificationType.PremiumBoostRevoked]               = new[] { "offerId" },
            [NotificationType.BenefitBudgetLow]                  = new[] { "plan", "remaining", "funded", "percent", "currency" },

            // ── Identity / Account ─────────────────────────────────────────
            [NotificationType.ProviderEmailVerification]         = new[] { "verifyUrl" },
            [NotificationType.OtpLoginCode]                      = new[] { "otp", "expiresMinutes", "maskedTarget" },
            [NotificationType.PasswordRecoveryOtp]               = new[] { "otp", "expiresMinutes", "maskedTarget" },
            [NotificationType.ProfileApproved]                   = new[] { "profileType" },
            [NotificationType.ProfileRejected]                   = new[] { "profileType", "reason" },
            [NotificationType.ProfileSuspended]                  = new[] { "reason" },
            [NotificationType.OnboardingRevisionRequested]       = new[] { "steps", "note" },

            // ── Messaging ──────────────────────────────────────────────────
            [NotificationType.NewMessageReceived]                = new[] { "senderName", "conversationTitle" },
            [NotificationType.SupportRequestOpened]              = new[] { "topic", "subject", "requesterName" },

            // ── Content ────────────────────────────────────────────────────
            [NotificationType.ContentPublished]                  = new[] { "contentType", "slug" },

            // ── CargoDry ───────────────────────────────────────────────────
            [NotificationType.CargoDryKitActivated]              = new[] { "kitCode", "serialNumber", "productName", "expiryDate" },
            [NotificationType.CargoDryKitExpiringReminder]       = new[] { "kitCode", "daysLeft", "expiryDate", "productName" },
            [NotificationType.CargoDryKitExpired]                = new[] { "kitCode" },
            [NotificationType.CargoDryKitRenewed]                = new[] { "kitCode", "newExpiryDate", "renewalType" },
            [NotificationType.CargoDryKitRevoked]                = new[] { "kitCode", "revokedAt", "reason" },
            [NotificationType.CargoDryRenewalNotificationRequested] = new[]
            {
                "kitCode", "renewalCode", "productCode", "productName", "daysUntilExpiry", "expiryDate",
                "renewalPrice", "languageCode", "templateCode", "recipientEmail", "recipientPhone",
            },
            // Dört CargoDry sağlayıcı-milestone tipi aynı üç anahtarı kullanır.
            [NotificationType.CargoDryProviderFirstSale]             = new[] { "displayValue", "milestoneType", "periodKey" },
            [NotificationType.CargoDryProviderMonthlyTargetReached]  = new[] { "displayValue", "milestoneType", "periodKey" },
            [NotificationType.CargoDryProviderTierUp]                = new[] { "displayValue", "milestoneType", "periodKey" },
            [NotificationType.CargoDryProviderStreakMilestone]       = new[] { "displayValue", "milestoneType", "periodKey" },
        };

    /// <summary>Tip için izin verilen placeholder adları (bilinmiyorsa boş liste).</summary>
    public static IReadOnlyList<string> GetAllowedPlaceholders(NotificationType type)
        => _map.TryGetValue(type, out var keys) ? keys : Array.Empty<string>();

    /// <summary>Bir anahtarın verilen tip için katalogda tanımlı olup olmadığı.</summary>
    public static bool IsKnownPlaceholder(NotificationType type, string key)
        => _map.TryGetValue(type, out var keys) && keys.Contains(key);

    /// <summary>Katalogda tanımlı tüm tipler (test/tur amaçlı).</summary>
    public static IReadOnlyCollection<NotificationType> KnownTypes => (IReadOnlyCollection<NotificationType>)_map.Keys;
}
