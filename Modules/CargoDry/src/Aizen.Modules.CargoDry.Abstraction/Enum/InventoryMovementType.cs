namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Describes the type of a CargoDry inventory movement ledger entry.
/// Every stock-state change produces one immutable movement row.
/// Positive quantity = stock increase; negative = stock decrease.
/// </summary>
public enum InventoryMovementType
{
    /// <summary>
    /// A batch was allocated to a provider. Positive quantity = number of kits allocated.
    /// Phase 2 — provider inventory allocation.
    /// </summary>
    BatchAllocated   = 1,

    /// <summary>
    /// A kit held by the provider was activated by an end user on a vessel.
    /// Negative quantity (stock leaves the provider's available pool).
    /// </summary>
    KitActivated     = 2,

    /// <summary>
    /// A kit held by the provider was revoked by admin.
    /// Negative quantity.
    /// </summary>
    KitRevoked       = 3,

    /// <summary>
    /// A kit was physically returned from the provider back to Inktavia.
    /// Negative quantity in provider's pool; a separate PlatformWarehouse movement may be created.
    /// </summary>
    KitReturned      = 4,

    /// <summary>
    /// A kit was transferred from one provider to another.
    /// Creates one negative movement for the source provider and one positive for the destination.
    /// </summary>
    KitTransferred   = 5,

    /// <summary>
    /// Admin manual correction to provider stock.
    /// Quantity can be positive (stock increase) or negative (stock decrease).
    /// AvailableStock must not go negative after the adjustment.
    /// </summary>
    ManualAdjustment = 6,
}
