namespace Aizen.Bff.MarineProvider.Application.Jobs;

internal static class RefitErrorHelper
{
    internal static string? ExtractError(Refit.ApiException ex)
    {
        if (string.IsNullOrWhiteSpace(ex.Content)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(ex.Content);
            if (doc.RootElement.TryGetProperty("header", out var h) && h.TryGetProperty("errorMessage", out var m))
                return m.GetString();
        }
        catch { }
        return null;
    }
}
