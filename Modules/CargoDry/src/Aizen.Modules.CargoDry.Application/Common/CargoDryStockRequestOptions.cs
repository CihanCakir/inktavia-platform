using Aizen.Modules.CargoDry.Abstraction.RemoteCall;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.CargoDry.Application.Common;

/// <summary>
/// Config for the stock-request auto-receive sweep. Days after Ship before a Shipped request auto-transitions to
/// Received when the provider never confirms. Read once at ship time and FROZEN onto the request (the sweep never
/// re-reads config), mirroring the SR-side CargoDry.* freeze-at-action convention.
///
/// Resolution order: the admin-editable ReferenceData system parameter <c>CargoDry.StockRequestAutoReceiveDays</c>
/// (same mechanism as the SR-side CargoDry.* keys, via <see cref="ICargoDryReferenceDataRemoteCall"/>), then host
/// config <c>CargoDry:StockRequestAutoReceiveDays</c>, then the hardcoded default. All failures degrade to the next
/// source (never throws).
/// </summary>
public static class CargoDryStockRequestOptions
{
    public const string ReferenceDataKey       = "CargoDry.StockRequestAutoReceiveDays";
    public const string ConfigKey              = "CargoDry:StockRequestAutoReceiveDays";
    public const int    DefaultAutoReceiveDays = 7;

    /// <summary>Host-config-only resolution (fallback when no ReferenceData call is available).</summary>
    public static int ResolveAutoReceiveDays(IConfiguration configuration)
    {
        var value = configuration?.GetValue<int?>(ConfigKey);
        return value is > 0 ? value.Value : DefaultAutoReceiveDays;
    }

    /// <summary>
    /// Admin-editable resolution: ReferenceData system parameter first (positive int + active), then host config,
    /// then the default. Any exception from the remote call degrades to the config/default path.
    /// </summary>
    public static async Task<int> ResolveAutoReceiveDaysAsync(
        ICargoDryReferenceDataRemoteCall referenceData, IConfiguration configuration, CancellationToken ct = default)
    {
        try
        {
            var dto = (await referenceData.GetSystemParameter(ReferenceDataKey)).Body;
            if (dto is { IsActive: true } && int.TryParse(dto.Value, out var days) && days > 0)
                return days;
        }
        catch
        {
            // ReferenceData unavailable / bad value → fall through to host config + default.
        }

        return ResolveAutoReceiveDays(configuration);
    }
}
