using System.ComponentModel.DataAnnotations;

namespace Aizen.Modules.Notification.Application.Command.SubscribeNewsletter;

/// <summary>
/// P1/P4 — newsletter-specific settings (bound from <c>NewsletterIntake</c>). The spam threshold, IP-hash salt and rate
/// window are DELIBERATELY reused from <c>ContactIntakeOptions</c> (same scorer/policy) — this class only carries what
/// is unique to the newsletter: the confirmation-token lifetime and the public-site links the emails point at.
///
/// P4 — the links are now per-locale and supplied by configuration. The public website owns its own route segments
/// (next-intl, <c>localePrefix: 'always'</c>, no unprefixed page), so the backend is TOLD the paths per locale and
/// never derives them. There is no baked-in default origin: a misconfigured deployment must fail to boot rather than
/// silently mail links to the wrong domain.
/// </summary>
public sealed class NewsletterIntakeOptions : IValidatableObject
{
    public const string SectionName = "NewsletterIntake";

    /// <summary>Lifetime of a confirmation token (hours). After this the token is expired and confirm fails.</summary>
    public int ConfirmationTokenTtlHours { get; set; } = 48;

    /// <summary>
    /// Public website origin the confirm/unsubscribe links point at. REQUIRED — no default; the deployment fails to
    /// start when absent (see <see cref="Validate"/> + <c>ValidateOnStart</c>).
    /// </summary>
    [Required(ErrorMessage = "NewsletterIntake:PublicSiteBaseUrl is required.")]
    public string PublicSiteBaseUrl { get; set; } = default!;

    /// <summary>
    /// Locale code → the two paths on the public site. Keys are language codes ("tr", "en"); matched case-insensitively.
    /// </summary>
    public Dictionary<string, NewsletterLinkPaths> Links { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Locale used when a subscriber's <c>Lang</c> has no entry in <see cref="Links"/>.</summary>
    public string FallbackLang { get; set; } = "tr";

    /// <summary>
    /// Fail-closed rule: the <see cref="FallbackLang"/> entry in <see cref="Links"/> must exist with non-empty paths,
    /// otherwise even the fallback branch would mail a broken link. Runs under <c>ValidateDataAnnotations</c> at startup.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(FallbackLang))
        {
            yield return new ValidationResult(
                "NewsletterIntake:FallbackLang is required.", new[] { nameof(FallbackLang) });
            yield break;
        }

        if (Links is null
            || !Links.TryGetValue(FallbackLang, out var fallback)
            || fallback is null
            || string.IsNullOrWhiteSpace(fallback.Confirm)
            || string.IsNullOrWhiteSpace(fallback.Unsubscribe))
        {
            yield return new ValidationResult(
                $"NewsletterIntake:Links must contain the fallback locale '{FallbackLang}' with non-empty Confirm and Unsubscribe paths.",
                new[] { nameof(Links) });
        }
    }
}

/// <summary>The two public-site paths for one locale (each gets <c>?token=</c> appended when the link is built).</summary>
public sealed class NewsletterLinkPaths
{
    public string Confirm { get; set; } = default!;
    public string Unsubscribe { get; set; } = default!;
}
