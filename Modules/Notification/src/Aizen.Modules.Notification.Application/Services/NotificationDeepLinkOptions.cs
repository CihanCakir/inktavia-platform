namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Base URLs / scheme for rendering per-audience notification deep links. Bound from the "Notification" section
/// (env: Notification__ProviderWebBaseUrl, Notification__AdminWebBaseUrl, Notification__OwnerWebBaseUrl,
/// Notification__MobileScheme). Absent values degrade gracefully (deep link omitted / relative path only).
///
/// Ops must set (see the report): Notification__ProviderWebBaseUrl, Notification__AdminWebBaseUrl and add the
/// corresponding hosts to Notifications__DeepLink__AllowedHosts, plus Notifications__DeepLink__AllowedSchemes__0=inktavia-marine.
/// </summary>
public sealed class NotificationDeepLinkOptions
{
    public const string SectionName = "Notification";

    /// <summary>Provider web base, e.g. https://provider.inktavia.com (no trailing slash).</summary>
    public string? ProviderWebBaseUrl { get; set; }

    /// <summary>Admin web base, e.g. https://admin.inktavia.com (no trailing slash).</summary>
    public string? AdminWebBaseUrl { get; set; }

    /// <summary>Owner web landing base for the email https fallback line. Not live yet (wave 4D) — leave unset to omit the fallback.</summary>
    public string? OwnerWebBaseUrl { get; set; }

    /// <summary>
    /// Base for OWNER email links: the mobile-BFF public deep-link bridge (e.g. https://m.inktavia.com/link). Owners are
    /// mobile-only, so owner email links point here; the bridge redirects to the app scheme with an install fallback.
    /// Unset ⇒ owner email links fall back to <see cref="WebBaseUrl"/> (generic).
    /// </summary>
    public string? OwnerLinkBaseUrl { get; set; }

    /// <summary>Owner mobile deep-link scheme (no ://). Default "inktavia-marine".</summary>
    public string MobileScheme { get; set; } = "inktavia-marine";

    /// <summary>
    /// Web base used to absolutize a ROOT-RELATIVE deep link when embedding it in an email body (emails need a full
    /// URL to be clickable). Unset ⇒ relative deep links are not embedded in email. NOTE (flag): a single base is used
    /// for all audiences on the email channel; per-audience hosts (provider vs admin vs owner web) is a follow-up.
    /// </summary>
    public string? WebBaseUrl { get; set; }
}
