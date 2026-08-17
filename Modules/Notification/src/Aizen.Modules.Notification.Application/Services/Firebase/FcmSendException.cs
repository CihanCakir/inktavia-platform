namespace Aizen.Modules.Notification.Application.Services.Firebase;

/// <summary>
/// BE-MO9a — a classified FCM send failure (adapts Metropol's PushException). Carries the <see cref="ErrorType"/> and
/// whether the offending token was deactivated. Never contains the token value or the service-account.
/// </summary>
public sealed class FcmSendException : Exception
{
    public FcmSendException(string message, FcmErrorType errorType, bool tokenDeactivated, Exception? inner = null)
        : base(message, inner)
    {
        ErrorType = errorType;
        TokenDeactivated = tokenDeactivated;
    }

    public FcmErrorType ErrorType { get; }
    public bool TokenDeactivated { get; }
}

/// <summary>Summary of a multicast send — never throws on a single bad token; per-token outcomes are tallied here.</summary>
public sealed record FcmMulticastResult(int SuccessCount, int FailedCount, int DeactivatedCount)
{
    public static readonly FcmMulticastResult Empty = new(0, 0, 0);
}
