namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

/// <summary>
/// The value type of a <c>PricingAttributeDefinition</c> (S2a, §20.6). <b>Descriptive metadata only</b> — attributes
/// record the pricing-relevant variables an offer line carries (engine install type, paint type, work difficulty…);
/// they never enter the line money math or the S1/S6/S7/S8 8-equality. <see cref="Lookup"/> attributes bind to an R4
/// marine lookup group (validated via the ReferenceData remote call); the others are free/number/boolean values.
/// </summary>
public enum PricingAttributeDataType
{
    Lookup  = 1,
    Number  = 2,
    Text    = 3,
    Boolean = 4,
}
