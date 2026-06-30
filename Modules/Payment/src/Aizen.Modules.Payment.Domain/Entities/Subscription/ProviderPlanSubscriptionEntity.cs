using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Subscription;

[DocumentationInfo("Provider plan subscription entity",
    "Active subscription record linking a provider to their chosen plan for a billing period.")]
public sealed class ProviderPlanSubscriptionEntity : AizenEntityWithAudit
{
    public long               ProviderProfileId          { get; private set; }
    public long               ProviderPlanId             { get; private set; }
    public SubscriptionStatus Status                     { get; private set; }
    public decimal            PaidAmount                 { get; private set; }
    public string             CurrencyCode               { get; private set; } = "TRY";
    public DateTime           SubscriptionPeriodStart    { get; private set; }
    public DateTime           SubscriptionPeriodEnd      { get; private set; }
    public bool               AutoRenew                  { get; private set; }
    public long?              PaymentTransactionId       { get; private set; }
    public decimal            CommissionRateAtSubscription { get; private set; }  // Snapshot at sub time
    public DateTime?          CancelledAt                { get; private set; }
    public string?            CancellationReason         { get; private set; }

    private ProviderPlanSubscriptionEntity() { }

    public static ProviderPlanSubscriptionEntity Create(
        long providerProfileId, long providerPlanId,
        decimal paidAmount, string currencyCode,
        DateTime periodStart, DateTime periodEnd,
        bool autoRenew, long? paymentTransactionId,
        decimal commissionRateAtSubscription)
    {
        return new ProviderPlanSubscriptionEntity
        {
            ProviderProfileId             = providerProfileId,
            ProviderPlanId                = providerPlanId,
            Status                        = SubscriptionStatus.Active,
            PaidAmount                    = paidAmount,
            CurrencyCode                  = currencyCode.ToUpperInvariant(),
            SubscriptionPeriodStart       = periodStart,
            SubscriptionPeriodEnd         = periodEnd,
            AutoRenew                     = autoRenew,
            PaymentTransactionId          = paymentTransactionId,
            CommissionRateAtSubscription  = commissionRateAtSubscription,
            IsActive                      = true,
        };
    }

    public void Cancel(string? reason)
    {
        Status              = SubscriptionStatus.Cancelled;
        CancelledAt         = DateTime.UtcNow;
        CancellationReason  = reason;
    }

    public void MarkExpired() => Status = SubscriptionStatus.Expired;

    public void MarkPastDue() => Status = SubscriptionStatus.PastDue;

    public bool IsCurrentlyActive(DateTime utcNow) =>
        Status == SubscriptionStatus.Active &&
        SubscriptionPeriodStart <= utcNow &&
        SubscriptionPeriodEnd >= utcNow;
}
