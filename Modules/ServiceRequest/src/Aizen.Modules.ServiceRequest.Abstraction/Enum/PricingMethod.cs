namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

/// <summary>
/// How a line's price was derived (§20.4). <b>Descriptive metadata only</b> — S1 keeps the money math unchanged
/// (<c>LineSubtotal = Round(Quantity × UnitPrice)</c> for every method). The FE offer builder and S2/S4
/// (attributes/travel) interpret <c>Quantity</c> per method (hours for PerHour, km for PerKm, …).
/// EstimateRange / AfterInspection (non-fixed offers) are stored here but their offer semantics are S11.
/// </summary>
public enum PricingMethod
{
    Fixed            = 1,
    PerUnit          = 2,
    PerHour          = 3,
    PerDay           = 4,
    PerKm            = 5,
    PerLiter         = 6,
    PerSquareMeter   = 7,
    EstimateRange    = 8,
    AfterInspection  = 9,
    TimeAndMaterials = 10,
}
