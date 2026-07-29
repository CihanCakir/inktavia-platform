namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Who funds a customer discount (§19.6). A discount without a funding source cannot be applied.
/// Platform ↔ Provider are never auto-shifted; ProviderFunded requires prior explicit provider consent.
/// SupplierFunded is reserved for a future phase. Stable int (HasConversion&lt;int&gt;).
/// </summary>
public enum CustomerDiscountFundingMode
{
    PlatformFunded = 1,
    ProviderFunded = 2,
    Shared         = 3,   // PlatformFundingRate + ProviderFundingRate == 1.0
    SupplierFunded = 4,   // reserved (not applied in P6)
}
