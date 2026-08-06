using System.Text;
using System.Text.Json;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.AdminPanel.Application.Common.Http;

/// <summary>
/// Fail-envelope fidelity handler for the AdminPanel BFF → Payment module calls (FIX_RULE_CONFLICT_ENVELOPE).
///
/// The Payment module surfaces business errors (e.g. <c>*RuleConflict</c> / <c>*Invalid</c>) as an
/// <c>AizenBusinessException</c> which its global middleware maps to <b>HTTP 400 + a fail envelope</b>
/// (<c>{ header: { isSuccess:false, errorCode, errorMessage } }</c>). Without this handler, Refit sees the raw 400
/// and throws a <c>Refit.ApiException</c>; that is NOT an <c>AizenBusinessException</c>, so the AdminPanel's own
/// global middleware maps it to a generic <b>500</b> and the module's <c>errorCode</c>/<c>errorMessage</c> are lost —
/// the FE's <c>RuleConflictBanner</c> then degrades to the generic "rejected" text.
///
/// This handler intercepts a non-success response whose body is an Aizen fail envelope and re-throws it as the BFF's
/// own <see cref="AizenBusinessException"/> (preserving the numeric <c>errorCode</c> + localized <c>errorMessage</c>),
/// so the AdminPanel middleware returns a structured <b>400</b> the FE can recognize. Non-envelope errors pass through
/// unchanged (Refit's normal <c>ApiException</c> behavior is preserved).
///
/// SCOPE: registered ONLY on the <c>IPaymentRemoteCall</c> HttpClient chain. It does NOT touch
/// <c>Core.RemoteCall</c> internals or the global exception middleware, and does not affect any other module's client.
/// </summary>
[DocumentationInfo("Admin payment BFF fail-envelope handler",
    "Converts a Payment-module fail envelope (HTTP 4xx/5xx with { header.isSuccess=false, errorCode, errorMessage }) " +
    "into an AizenBusinessException so the AdminPanel middleware returns a structured 400 preserving the module's " +
    "error code + message. Scoped to the IPaymentRemoteCall client only; non-envelope errors pass through.")]
public sealed class AdminPaymentBffFailEnvelopeHandler : DelegatingHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        // Only inspect failures; success responses flow straight through untouched.
        if (response.IsSuccessStatusCode || response.Content is null)
            return response;

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return response;

        // Buffer the body once so we can either translate it (throw) or hand it back to Refit unchanged.
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (TryReadFailEnvelope(raw, out var errorCode, out var errorMessage))
        {
            // Re-throw as the BFF's own fail-loud business error → AdminPanel global middleware returns a 400
            // envelope carrying this errorCode + errorMessage. This is what every admin rule screen (P2–P7) reads.
            throw new AizenBusinessException(errorCode, errorMessage);
        }

        // Not an Aizen fail envelope — restore the consumed body so Refit's normal error handling still applies.
        // Content-Type + Content-Length are managed by StringContent; copy only the remaining content headers.
        var restored = new StringContent(raw, Encoding.UTF8, mediaType ?? "application/json");
        foreach (var header in response.Content.Headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;
            restored.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        response.Content = restored;
        return response;
    }

    /// <summary>
    /// Parses <paramref name="body"/> as an Aizen envelope and, when it is a genuine fail envelope
    /// (<c>header.isSuccess == false</c> with a non-zero <c>errorCode</c>), yields the module's code + message.
    /// </summary>
    private static bool TryReadFailEnvelope(string body, out int errorCode, out string errorMessage)
    {
        errorCode = 0;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            if (!doc.RootElement.TryGetProperty("header", out var header) ||
                header.ValueKind != JsonValueKind.Object)
                return false;

            // A fail envelope must explicitly say isSuccess == false.
            if (!header.TryGetProperty("isSuccess", out var isSuccess) ||
                isSuccess.ValueKind != JsonValueKind.False)
                return false;

            if (!header.TryGetProperty("errorCode", out var code) ||
                code.ValueKind != JsonValueKind.Number ||
                !code.TryGetInt32(out errorCode) ||
                errorCode == 0)
                return false;

            errorMessage = header.TryGetProperty("errorMessage", out var msg) &&
                           msg.ValueKind == JsonValueKind.String
                ? msg.GetString() ?? string.Empty
                : string.Empty;

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
