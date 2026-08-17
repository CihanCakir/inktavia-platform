using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Subscription;

[DocumentationInfo("Participant plan subscription entity",
    "Active subscription record linking a participant to their chosen plan for a billing period.")]
public sealed class ParticipantPlanSubscriptionEntity : AizenEntityWithAudit
{
    public long               ParticipantProfileId       { get; private set; }
    public long               ParticipantPlanId          { get; private set; }
    public SubscriptionStatus Status                     { get; private set; }
    public decimal            PaidAmount                 { get; private set; }
    public string             CurrencyCode               { get; private set; } = "TRY";
    public DateTime           SubscriptionPeriodStart    { get; private set; }
    public DateTime           SubscriptionPeriodEnd      { get; private set; }
    public bool               AutoRenew                  { get; private set; }
    public long?              PaymentTransactionId       { get; private set; }
    public decimal            ServiceDiscountAtSubscription  { get; private set; }  // Snapshot
    public decimal            EarnMultiplierAtSubscription   { get; private set; }  // Snapshot
    public DateTime?          CancelledAt                { get; private set; }
    public string?            CancellationReason         { get; private set; }

    private ParticipantPlanSubscriptionEntity() { }

    public static ParticipantPlanSubscriptionEntity Create(
        long participantProfileId, long participantPlanId,
        decimal paidAmount, string currencyCode,
        DateTime periodStart, DateTime periodEnd,
        bool autoRenew, long? paymentTransactionId,
        decimal serviceDiscountAtSubscription, decimal earnMultiplierAtSubscription)
    {
        return new ParticipantPlanSubscriptionEntity
        {
            ParticipantProfileId              = participantProfileId,
            ParticipantPlanId                 = participantPlanId,
            Status                            = SubscriptionStatus.Active,
            PaidAmount                        = paidAmount,
            CurrencyCode                      = currencyCode.ToUpperInvariant(),
            SubscriptionPeriodStart           = periodStart,
            SubscriptionPeriodEnd             = periodEnd,
            AutoRenew                         = autoRenew,
            PaymentTransactionId              = paymentTransactionId,
            ServiceDiscountAtSubscription     = serviceDiscountAtSubscription,
            EarnMultiplierAtSubscription      = earnMultiplierAtSubscription,
            IsActive                          = true,
        };
    }

    public void Cancel(string? reason)
    {
        Status             = SubscriptionStatus.Cancelled;
        CancelledAt        = DateTime.UtcNow;
        CancellationReason = reason;
    }

    public void MarkPastDue() => Status = SubscriptionStatus.PastDue;

    public void MarkExpired() => Status = SubscriptionStatus.Expired;

    public bool IsCurrentlyActive(DateTime utcNow) =>
        Status == SubscriptionStatus.Active &&
        SubscriptionPeriodStart <= utcNow &&
        SubscriptionPeriodEnd >= utcNow;
}
