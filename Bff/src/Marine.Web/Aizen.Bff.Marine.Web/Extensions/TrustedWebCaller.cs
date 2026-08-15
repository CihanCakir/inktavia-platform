using System.Security.Cryptography;
using System.Text;
using Aizen.Bff.Marine.Web.Application.Common.Options;

namespace Aizen.Bff.Marine.Web.Extensions;

/// <summary>
/// The trusted server-caller decision behind the <c>public-read-ip</c> rate-limit policy (W2.1). Extracted from the
/// inline policy so it can be unit-tested: a valid <c>X-Aizen-Web-Caller</c> secret routes the caller to the shared
/// high/unlimited <see cref="TrustedPartitionKey"/> partition; everyone else stays on the per-IP window. The compare
/// is constant-time (<see cref="CryptographicOperations.FixedTimeEquals"/>), and an empty/unset configured secret is
/// fail-closed — no header value can ever be "trusted".
/// </summary>
public static class TrustedWebCaller
{
    /// <summary>Shared partition key for all trusted server-side callers (their handful of IPs collapse to one).</summary>
    public const string TrustedPartitionKey = "trusted-web-caller";

    /// <summary>Partition key used when an untrusted caller's remote IP cannot be determined.</summary>
    public const string UnknownIpPartitionKey = "unknown";

    /// <summary>
    /// True only when a non-empty <paramref name="secret"/> is configured AND the request presents a
    /// <c>X-Aizen-Web-Caller</c> header whose value equals it under a constant-time compare.
    /// </summary>
    public static bool IsTrusted(HttpContext httpContext, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        var provided = httpContext.Request.Headers[MarineWebPublicOptions.TrustedCallerHeader].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(provided)
               && CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(secret));
    }

    /// <summary>
    /// The rate-limit partition key for this request: the shared trusted partition for a valid secret, otherwise the
    /// caller's remote IP (the strict per-IP protection, unchanged).
    /// </summary>
    public static string PartitionKeyFor(HttpContext httpContext, string? secret)
        => IsTrusted(httpContext, secret)
            ? TrustedPartitionKey
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownIpPartitionKey;
}
