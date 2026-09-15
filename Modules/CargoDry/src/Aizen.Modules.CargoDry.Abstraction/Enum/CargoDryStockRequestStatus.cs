namespace Aizen.Modules.CargoDry.Abstraction.Enum;

public enum CargoDryStockRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    /// <summary>Legacy terminal state (v1 dead-code path). New lifecycle uses Shipped → Received instead.</summary>
    Fulfilled = 4,
    Cancelled = 5,
    // Wave 4A — lifecycle revival.
    Shipped = 6,
    Received = 7,
}
