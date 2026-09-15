using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Builds per-audience deep links for notification templates (rendered via the <c>{{deepLink}}</c> variable).
/// Provider/Admin → configured web base + path (validated as http/https host allowlist). Owner → mobile scheme
/// deep link (validated via the scheme allowlist), with a separate https fallback line for email bodies.
/// </summary>
public interface INotificationDeepLinkBuilder
{
    /// <summary>Provider portal SR/order page: {ProviderWebBaseUrl}/app/service-requests/{id} (falls back to a relative path).</summary>
    string ProviderServiceRequest(long serviceRequestId);
    /// <summary>Provider portal stock-requests page.</summary>
    string ProviderStockRequests();
    /// <summary>Admin order detail page.</summary>
    string AdminSupplyOrder(long serviceRequestId);
    /// <summary>Admin awaiting-shipment / cargo fulfilment queue.</summary>
    string AdminAwaitingShipment();
    /// <summary>Admin stock-requests queue.</summary>
    string AdminStockRequests();
    /// <summary>Owner mobile deep link: inktavia-marine://service-requests/{id}.</summary>
    string OwnerServiceRequest(long serviceRequestId);
    /// <summary>Owner email https fallback line (empty when no owner web base is configured — wave 4D).</summary>
    string OwnerServiceRequestWebFallback(long serviceRequestId);
}

public sealed class NotificationDeepLinkBuilder : INotificationDeepLinkBuilder
{
    private readonly NotificationDeepLinkOptions _options;

    public NotificationDeepLinkBuilder(IOptions<NotificationDeepLinkOptions> options) => _options = options.Value;

    public string ProviderServiceRequest(long serviceRequestId)
        => Web(_options.ProviderWebBaseUrl, $"/app/service-requests/{serviceRequestId}");

    public string ProviderStockRequests()
        => Web(_options.ProviderWebBaseUrl, "/app/cargodry/stock-requests");

    public string AdminSupplyOrder(long serviceRequestId)
        => Web(_options.AdminWebBaseUrl, $"/app/cargodry/supply/orders/{serviceRequestId}");

    public string AdminAwaitingShipment()
        => Web(_options.AdminWebBaseUrl, "/app/cargodry/supply/orders?status=awaiting");

    public string AdminStockRequests()
        => Web(_options.AdminWebBaseUrl, "/app/cargodry/stock-requests");

    public string OwnerServiceRequest(long serviceRequestId)
        => $"{Scheme}://service-requests/{serviceRequestId}";

    public string OwnerServiceRequestWebFallback(long serviceRequestId)
        => string.IsNullOrWhiteSpace(_options.OwnerWebBaseUrl)
            ? string.Empty
            : $"{Trim(_options.OwnerWebBaseUrl)}/service-requests/{serviceRequestId}";

    private string Scheme => string.IsNullOrWhiteSpace(_options.MobileScheme) ? "inktavia-marine" : _options.MobileScheme.Trim();

    // Absolute {base}{path} when a base is configured; otherwise the relative path (always allowed by the validator).
    private static string Web(string? baseUrl, string path)
        => string.IsNullOrWhiteSpace(baseUrl) ? path : $"{Trim(baseUrl)}{path}";

    private static string Trim(string url) => url.TrimEnd('/');
}
