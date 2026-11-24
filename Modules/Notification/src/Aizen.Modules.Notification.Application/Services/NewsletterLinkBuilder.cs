using Aizen.Modules.Notification.Application.Command.SubscribeNewsletter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// P4 — builds the locale-correct public-site confirm/unsubscribe links the newsletter mails point at. The website
/// owns its route segments, so the paths come from <see cref="NewsletterIntakeOptions.Links"/> (per locale) and are
/// never derived here. A subscriber whose locale is missing from the map is NOT a boot failure (the fallback entry is
/// validated at startup): we fall back to <see cref="NewsletterIntakeOptions.FallbackLang"/> and log a warning naming
/// the missing locale — never the e-mail address or the token.
/// </summary>
public interface INewsletterLinkBuilder
{
    string BuildConfirmUrl(string lang, string token);
    string BuildUnsubscribeUrl(string lang, string token);
}

public sealed class NewsletterLinkBuilder : INewsletterLinkBuilder
{
    private readonly NewsletterIntakeOptions _options;
    private readonly ILogger<NewsletterLinkBuilder> _logger;

    public NewsletterLinkBuilder(IOptions<NewsletterIntakeOptions> options, ILogger<NewsletterLinkBuilder> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string BuildConfirmUrl(string lang, string token)
        => BuildUrl(ResolvePaths(lang).Confirm, token);

    public string BuildUnsubscribeUrl(string lang, string token)
        => BuildUrl(ResolvePaths(lang).Unsubscribe, token);

    private NewsletterLinkPaths ResolvePaths(string? lang)
    {
        var key = (lang ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(key) && _options.Links.TryGetValue(key, out var paths))
            return paths;

        // Unknown locale is a runtime fallback, not a boot failure (the fallback entry is validated at startup).
        _logger.LogWarning(
            "Newsletter link locale '{Locale}' is not configured; falling back to '{FallbackLang}'.",
            key, _options.FallbackLang);

        return _options.Links[_options.FallbackLang];
    }

    private string BuildUrl(string path, string token)
    {
        var baseUrl = _options.PublicSiteBaseUrl.TrimEnd('/');
        var p = path.StartsWith('/') ? path : "/" + path;
        return $"{baseUrl}{p}?token={Uri.EscapeDataString(token)}";
    }
}
