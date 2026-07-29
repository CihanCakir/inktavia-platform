namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>Lifecycle of a benefit budget instance (§19.7).</summary>
public enum CustomerBenefitBudgetStatus
{
    Active = 1,
    Closed = 2,
}

/// <summary>Lifecycle of a single benefit reservation (§19.7).</summary>
public enum CustomerBenefitReservationStatus
{
    Reserved = 1,
    Consumed = 2,
    Released = 3,
}

/// <summary>What happens to consumed budget on refund (§19.7-4; restore hook wired in P10).</summary>
public enum BenefitRefundRestorePolicy
{
    Restore = 1,   // consumed budget is restored to Remaining on refund
    Consume = 2,   // consumed budget stays consumed on refund
}
