namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Indicates the physical/logical stock location of a CargoDry kit at any point in time.
/// Transitions: PlatformWarehouse → ProviderWarehouse (on batch allocation) → Activated (on activation).
/// </summary>
public enum StockLocationType
{
    /// <summary>Kit is in Inktavia's own warehouse / internal stock. Default state after generation.</summary>
    PlatformWarehouse  = 1,

    /// <summary>Kit has been allocated to a provider's warehouse (consignment or resale).</summary>
    ProviderWarehouse  = 2,

    /// <summary>Kit is in transit between warehouse locations.</summary>
    Transit            = 3,

    /// <summary>Kit has been activated and is deployed on a vessel. No longer in inventory stock.</summary>
    Activated          = 4,
}
