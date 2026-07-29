namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Line-level commission dimension (§20.11). A commission rule may be scoped to a specific kind of
/// offer line so labour, parts, travel and pass-through items can carry different rates.
/// Null on a rule = applies to any line type. Integer values are stable (stored via HasConversion&lt;int&gt;).
/// </summary>
public enum LineType
{
    Labor       = 1,  // İşçilik
    Part        = 2,  // Parça
    Travel      = 3,  // Ulaşım / mobilizasyon
    PassThrough = 4,  // Pass-through (non-commissionable cost forwarded to the customer)
}
