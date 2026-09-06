using System.Text.Json;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Modules.Identity.Repository.Identity.Service.Onboarding;

/// <summary>
/// Phase-3 — the rules for the OperatingRegion step's OPTIONAL business coordinates. Centralized so the save-time
/// validator, the submit-time mirror, and the backfill seeder all agree. Enforcement mirrors cityCode's precedent:
/// validated WHEN PRESENT (never required for step completion; materialized on submit; distance pricing is null-safe
/// without them).
/// </summary>
public static class OnboardingCoordinateRules
{
    // Turkey mainland bbox, padded — a map-pin plausibility guard, not a precise border check.
    public const decimal TrMinLat = 35.0m, TrMaxLat = 43.0m, TrMinLng = 25.0m, TrMaxLng = 45.5m;

    /// <summary>Validate the optional coords in an OperatingRegion object: both-or-neither lat/lng + Turkey bbox.
    /// Throws <see cref="AizenBusinessException"/> on violation; a no-coords object passes.</summary>
    public static void Validate(JsonElement operatingRegion)
    {
        var hasLat = TryReadCoord(operatingRegion, "businessLatitude", out var lat);
        var hasLng = TryReadCoord(operatingRegion, "businessLongitude", out var lng);
        if (hasLat != hasLng)
            throw new AizenBusinessException("businessLatitude and businessLongitude must be provided together.");
        if (hasLat && hasLng && !(lat >= TrMinLat && lat <= TrMaxLat && lng >= TrMinLng && lng <= TrMaxLng))
            throw new AizenBusinessException("Business coordinates are outside the supported region (Turkey).");
    }

    /// <summary>Read both coords + optional label for materialization. Returns false unless BOTH numeric coords are
    /// present and in valid global ranges (so <c>SetBusinessLocation</c> never throws); the tighter Turkey bbox is a
    /// save-time concern.</summary>
    public static bool TryReadBusinessCoords(JsonElement operatingRegion, out decimal lat, out decimal lng, out string? label)
    {
        lat = 0m; lng = 0m; label = null;
        if (!TryReadCoord(operatingRegion, "businessLatitude", out lat)) return false;
        if (!TryReadCoord(operatingRegion, "businessLongitude", out lng)) return false;
        if (lat is < -90m or > 90m || lng is < -180m or > 180m) return false;
        label = operatingRegion.ValueKind == JsonValueKind.Object
                && operatingRegion.TryGetProperty("businessAddressLabel", out var al) && al.ValueKind == JsonValueKind.String
            ? al.GetString()
            : null;
        return true;
    }

    private static bool TryReadCoord(JsonElement obj, string name, out decimal value)
    {
        value = 0m;
        if (obj.ValueKind == JsonValueKind.Object
            && obj.TryGetProperty(name, out var el)
            && el.ValueKind == JsonValueKind.Number
            && el.TryGetDecimal(out var d))
        {
            value = d;
            return true;
        }
        return false;
    }
}
