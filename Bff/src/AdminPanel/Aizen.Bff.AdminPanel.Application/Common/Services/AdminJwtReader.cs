using System.Text.Json;

namespace Aizen.Bff.AdminPanel.Application.Common.Services;

/// <summary>
/// Minimal, dependency-free JWT payload reader — NO signature validation (the token was just minted by Keycloak on
/// our own refresh call, so it is already trusted). Used only to populate the login-response profile fields and the
/// refresh-token expiry for the admin refresh handler.
/// </summary>
internal static class AdminJwtReader
{
    public static (string? Email, string? GivenName, string? FamilyName) ReadProfile(string jwt)
    {
        using var doc = Parse(jwt);
        if (doc is null) return (null, null, null);
        var root = doc.RootElement;
        return (
            root.TryGetProperty("email", out var e) ? e.GetString() : null,
            root.TryGetProperty("given_name", out var g) ? g.GetString() : null,
            root.TryGetProperty("family_name", out var f) ? f.GetString() : null);
    }

    public static DateTime? ReadExpiryUtc(string jwt)
    {
        using var doc = Parse(jwt);
        if (doc is null) return null;
        return doc.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : null;
    }

    private static JsonDocument? Parse(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            return JsonDocument.Parse(Convert.FromBase64String(payload));
        }
        catch
        {
            return null;
        }
    }
}
