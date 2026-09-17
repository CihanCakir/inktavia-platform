namespace Aizen.Modules.CargoDry.Application.Common;

/// <summary>
/// Generates the human-readable sell-through settlement code encoding the approved grouping dimensions:
/// Provider + Currency + Product + Month.
/// Format: <c>STS-{providerProfileId}-{currencyCode}-{productCode}-{yyyyMM}</c> (e.g. STS-129-TRY-CD-BASIC-202607).
/// ProductCode is uppercased and spaces are replaced with dashes. Shared by the activation service (link-at-activation)
/// and the settlement automation's second-pass linker so both mint identical codes for the same period key.
/// </summary>
public static class CargoDrySettlementCode
{
    public static string Generate(
        long     providerProfileId,
        string   currencyCode,
        string   productCode,
        DateTime periodUtc)
    {
        var normalizedProduct  = productCode.ToUpperInvariant().Replace(" ", "-");
        var normalizedCurrency = currencyCode.ToUpperInvariant();
        return $"STS-{providerProfileId}-{normalizedCurrency}-{normalizedProduct}-{periodUtc:yyyyMM}";
    }
}
