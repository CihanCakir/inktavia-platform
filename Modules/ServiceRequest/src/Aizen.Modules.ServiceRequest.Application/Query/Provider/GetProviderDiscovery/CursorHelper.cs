using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDiscovery;

internal static class CursorHelper
{
    public static string Encode(ProviderDiscoveryItemDto lastItem, ProviderServiceRequestDiscoveryFilter filter)
    {
        var sortValue = filter.SortBy switch
        {
            "PriorityDesc" => lastItem.Priority,
            "DistanceAsc" => lastItem.DistanceKm?.ToString("F6") ?? "999999",
            _ => lastItem.PublishedAt?.ToString("O") ?? string.Empty,
        };

        var filterHash = ComputeFilterHash(filter);
        var raw = $"{sortValue}|{lastItem.Id}|{filterHash}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static (string sortValue, long lastId, string filterHash)? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|', 3);
            if (parts.Length != 3 || !long.TryParse(parts[1], out var lastId))
                return null;

            return (parts[0], lastId, parts[2]);
        }
        catch
        {
            return null;
        }
    }

    public static string ComputeFilterHash(ProviderServiceRequestDiscoveryFilter filter)
    {
        // Serialize filter excluding Cursor and PageSize for hash stability
        var hashInput = new
        {
            filter.SortBy,
            filter.LocationCityCode,
            filter.LocationCountryCode,
            filter.ServiceCategoryCode,
            filter.MinPriority,
            filter.SearchTerm,
            filter.OfferState,
            filter.CenterLatitude,
            filter.CenterLongitude,
            filter.RadiusKm,
            filter.BoundsMinLat,
            filter.BoundsMaxLat,
            filter.BoundsMinLng,
            filter.BoundsMaxLng,
        };

        var json = JsonSerializer.Serialize(hashInput);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes)[..16]; // first 16 hex chars
    }
}
