using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;

namespace Aizen.Modules.ServiceRequest.Application.Common;

/// <summary>
/// CargoDry supply v2 — admin-editable timeouts, read from ReferenceData system parameters (keys prefixed "CargoDry.")
/// with hardcoded fallbacks. Deadlines are FROZEN at the action moment (order-create / mark-delivered / mark-shipped),
/// so the sweep jobs compare stored deadlines and never re-read config — a config change never retro-shifts live orders.
/// </summary>
public static class CargoDrySupplyOptions
{
    public const string ProviderAcceptTimeoutHoursKey = "CargoDry.ProviderAcceptTimeoutHours";
    public const string DeliveredAutoCompleteHoursKey = "CargoDry.DeliveredAutoCompleteHours";
    public const string ShippedAutoCompleteDaysKey    = "CargoDry.ShippedAutoCompleteDays";

    public const int DefaultProviderAcceptTimeoutHours = 5;
    public const int DefaultDeliveredAutoCompleteHours = 24;
    public const int DefaultShippedAutoCompleteDays    = 7;

    public static Task<int> GetProviderAcceptTimeoutHoursAsync(IServiceRequestReferenceDataRemoteCall referenceData)
        => GetIntAsync(referenceData, ProviderAcceptTimeoutHoursKey, DefaultProviderAcceptTimeoutHours);

    public static Task<int> GetDeliveredAutoCompleteHoursAsync(IServiceRequestReferenceDataRemoteCall referenceData)
        => GetIntAsync(referenceData, DeliveredAutoCompleteHoursKey, DefaultDeliveredAutoCompleteHours);

    public static Task<int> GetShippedAutoCompleteDaysAsync(IServiceRequestReferenceDataRemoteCall referenceData)
        => GetIntAsync(referenceData, ShippedAutoCompleteDaysKey, DefaultShippedAutoCompleteDays);

    private static async Task<int> GetIntAsync(IServiceRequestReferenceDataRemoteCall referenceData, string key, int fallback)
    {
        try
        {
            var resp = await referenceData.GetSystemParameter(key);
            var dto = resp?.Body;
            if (dto is { IsActive: true } && int.TryParse(dto.Value, out var v) && v > 0)
                return v;
        }
        catch
        {
            // ReferenceData unreachable / bad value → fall back to the safe default (mirrors the commission-vat fallback).
        }
        return fallback;
    }
}
