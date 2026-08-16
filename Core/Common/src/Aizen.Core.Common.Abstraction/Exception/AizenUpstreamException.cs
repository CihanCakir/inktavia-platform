using Aizen.Core.Common.Abstraction.Exception;

namespace Aizen.Core.Infrastructure.Exception;

/// <summary>
/// A genuine downstream/infrastructure failure while calling another service (Identity / Keycloak /
/// file-storage): a non-success HTTP status (401/403/infra-404/5xx), a timeout, a transport error, or an
/// unparseable/empty body. Distinct from <see cref="AizenBusinessException"/> (a business rule → 400):
/// the global exception middleware maps this to <b>502 Bad Gateway</b> so an infra failure can never be
/// mistaken for a business result — and, critically, never masked as a synthetic 200.
///
/// The public message is a stable, non-leaking "temporarily unavailable"; the real downstream status and a
/// correlation id are logged server-side and echoed back only as an opaque correlation id (ErrorDetails).
/// </summary>
public sealed class AizenUpstreamException : AizenException
{
    /// <summary>Stable, human-greppable code for this failure class (surfaced in logs).</summary>
    public const string StableCode = "AUTH_UPSTREAM_ERROR";

    /// <summary>Numeric error code carried on the response envelope header (AizenErrorCode.AuthUpstreamError).</summary>
    public const int UpstreamErrorCode = 40200;

    /// <summary>The downstream HTTP status that triggered this (null for timeout/transport/parse failures).</summary>
    public int? UpstreamStatusCode { get; }

    /// <summary>Opaque id tying the client response to the server-side Error log line.</summary>
    public string CorrelationId { get; }

    public AizenUpstreamException(string correlationId, int? upstreamStatusCode = null, string? publicMessage = null)
        : base(UpstreamErrorCode, publicMessage ?? "Service temporarily unavailable. Please try again.")
    {
        CorrelationId = correlationId;
        UpstreamStatusCode = upstreamStatusCode;
        IsRollback = true;
    }
}
