namespace Aizen.Modules.ServiceRequest.Abstraction.Constants;

/// <summary>
/// Well-known SERVICE_PROVIDER_CATEGORY lookup codes that the ServiceRequest module treats specially.
/// The taxonomy itself is owned by ReferenceData (lookup group SERVICE_PROVIDER_CATEGORY); these constants
/// only pin the codes the SR flow branches on so we never scatter magic strings.
/// </summary>
public static class ServiceRequestServiceCategoryCodes
{
    /// <summary>
    /// CargoDry supply: owner requests a CargoDry moisture-protection kit at a FIXED retail price (no bidding).
    /// PrincipalSale — the platform is the merchant; the fulfilling provider is paid via the CargoDry
    /// sell-through settlement, never via the SR escrow. See <c>CargoDryProductCode</c> on the SR.
    /// </summary>
    public const string CargoDrySupply = "CARGODRY_SUPPLY";
}
