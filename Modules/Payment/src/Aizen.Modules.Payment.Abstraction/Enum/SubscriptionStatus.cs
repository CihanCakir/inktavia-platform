namespace Aizen.Modules.Payment.Abstraction.Enum;

public enum SubscriptionStatus
{
    Active    = 1,
    PastDue   = 2,  // Payment failed but within grace period
    Cancelled = 3,
    Expired   = 4,  // Period ended without renewal
}
