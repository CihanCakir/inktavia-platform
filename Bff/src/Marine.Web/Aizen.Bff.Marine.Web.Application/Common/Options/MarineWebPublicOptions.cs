namespace Aizen.Bff.Marine.Web.Application.Common.Options;

/// <summary>
/// Public-surface options for the Marine Web BFF's real consumer — the server-rendered Next.js site
/// (inktavia-marine-web). Covers the trusted server-caller credential (W2.1) and the outbound cache-revalidation
/// webhook (W2.2). All secrets are environment/secret-store only and are NEVER sent by a browser.
/// </summary>
public sealed class MarineWebPublicOptions
{
    public const string SectionName = "MarineWebPublic";

    /// <summary>
    /// Header a trusted server-side caller (the Next.js server) presents to bypass the per-IP public read limit.
    /// Mirrors the ecosystem's <c>X-Aizen-Bff-Assertion</c> shared-secret style — this is a SEPARATE header for a
    /// SEPARATE purpose (rate-limit tier selection, not identity assertion).
    /// </summary>
    public const string TrustedCallerHeader = "X-Aizen-Web-Caller";

    /// <summary>
    /// Shared secret the trusted server caller must present in <see cref="TrustedCallerHeader"/>. Empty/unset =
    /// disabled → every caller stays on the per-IP <c>public-read-ip</c> policy (fail-closed to the strict limit).
    /// Server-side only; never document it as something a browser sends.
    /// </summary>
    public string? TrustedCallerSecret { get; set; }

    /// <summary>Rate limit applied to trusted callers (shared across their handful of IPs, not per-IP).</summary>
    public TrustedRateLimitOptions TrustedRateLimit { get; set; } = new();

    /// <summary>Outbound cache-revalidation webhook to the Next.js server (bus-driven, best-effort).</summary>
    public RevalidateOptions Revalidate { get; set; } = new();

    /// <summary>
    /// SEO indexability thresholds behind the <c>ISeoIndexabilityPolicy</c> seam (W3.2). Config-managed so the SEO
    /// owners can change them without a deployment.
    /// FUTURE: migrate the policy source from these Options to managed data (ReferenceData SystemParameter) — a new
    /// ISeoIndexabilityPolicy implementation, with NO change at the call sites (handlers depend on the interface).
    /// </summary>
    public SeoOptions Seo { get; set; } = new();

    public sealed class SeoOptions
    {
        /// <summary>An item must be Published to be indexable.</summary>
        public bool RequirePublished { get; set; } = true;

        /// <summary>A non-empty title is required to be indexable.</summary>
        public bool RequireTitle { get; set; } = true;

        /// <summary>A non-empty description is required to be indexable.</summary>
        public bool RequireDescription { get; set; } = false;

        /// <summary>Minimum resolved-body length to be indexable (only enforced where the body is available).</summary>
        public int MinBodyLength { get; set; } = 200;
    }

    public sealed class TrustedRateLimitOptions
    {
        /// <summary>Permits per window for the shared trusted partition. <c>&lt;= 0</c> ⇒ effectively unlimited.</summary>
        public int PermitLimit { get; set; } = 6000;

        public int WindowSeconds { get; set; } = 60;
    }

    public sealed class RevalidateOptions
    {
        /// <summary>Absolute URL of the Next.js revalidation endpoint. Empty/unset ⇒ webhook disabled (no-op).</summary>
        public string? Url { get; set; }

        /// <summary>Shared secret sent to the Next.js revalidation endpoint (header <c>X-Aizen-Web-Caller</c>).</summary>
        public string? Secret { get; set; }
    }
}
