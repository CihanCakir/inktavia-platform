using System.Net.Http.Json;
using Aizen.Bff.Marine.Web.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.Marine.Web.Application.Common.Services;

/// <summary>
/// The cache-revalidation signal forwarded to the Next.js server when published content changes. <c>Lang</c> is
/// null because the Content publish/unpublish events are language-agnostic (one item spans all its translations);
/// the website revalidates the item's tag across every locale it renders.
/// </summary>
public sealed record WebRevalidationPayload(
    string EntityType,
    string Id,
    string Slug,
    string? Lang,
    string ChangeKind);

/// <summary>
/// Forwards a cache-revalidation call to the Next.js server so it can revalidate a cache tag instead of polling.
/// Best-effort by contract: a webhook failure is logged and swallowed — it must NEVER break the bus consume.
/// </summary>
public interface IWebRevalidationNotifier
{
    Task NotifyAsync(WebRevalidationPayload payload, CancellationToken cancellationToken = default);
}

internal sealed class WebRevalidationNotifier : IWebRevalidationNotifier
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MarineWebPublicOptions _options;
    private readonly ILogger<WebRevalidationNotifier> _logger;

    public WebRevalidationNotifier(
        IHttpClientFactory httpClientFactory,
        IOptions<MarineWebPublicOptions> options,
        ILogger<WebRevalidationNotifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(WebRevalidationPayload payload, CancellationToken cancellationToken = default)
    {
        var url = _options.Revalidate.Url;
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogDebug("Revalidation webhook disabled (no MarineWebPublic:Revalidate:Url); skipping {Kind} {Slug}.",
                payload.ChangeKind, payload.Slug);
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
            if (!string.IsNullOrWhiteSpace(_options.Revalidate.Secret))
                request.Headers.TryAddWithoutValidation(MarineWebPublicOptions.TrustedCallerHeader, _options.Revalidate.Secret);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Revalidation webhook for {Kind} {Slug} returned {Status}.",
                    payload.ChangeKind, payload.Slug, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            // Best-effort: never let a webhook failure fail the consume (the content is already published/unpublished).
            _logger.LogWarning(ex, "Revalidation webhook for {Kind} {Slug} failed; content change stands regardless.",
                payload.ChangeKind, payload.Slug);
        }
    }
}
