using FirebaseAdmin.Messaging;

namespace Aizen.Modules.Notification.Application.Services.Firebase;

/// <summary>Adapts the legacy push provider's PushErrorType — the outcome class of an FCM send failure.</summary>
public enum FcmErrorType
{
    Unknown,
    /// <summary>The device token is dead (Unregistered) or malformed (InvalidArgument) → deactivate it.</summary>
    InvalidToken,
    /// <summary>FCM is transiently unavailable / internal → retriable, keep the token.</summary>
    ServiceUnavailable,
    /// <summary>The token belongs to a different sender project (SenderIdMismatch) — it will never work here → deactivate.</summary>
    SenderMismatch,
    /// <summary>The service-account credential / auth config is wrong → fix ops config, keep the token.</summary>
    InvalidAuthConfiguration,
}

/// <summary>
/// BE-MO9a — pure classification of a <see cref="MessagingErrorCode"/> into an <see cref="FcmErrorType"/> and the
/// derived "should this token be deactivated?" decision. Kept static + FirebaseAdmin-free of any app state so it is
/// unit-testable without Firebase credentials. Mirrors the legacy <c>CreateException</c> mapping.
/// </summary>
public static class FcmErrorClassifier
{
    public static FcmErrorType Classify(MessagingErrorCode code) => code switch
    {
        MessagingErrorCode.Unregistered      => FcmErrorType.InvalidToken,
        MessagingErrorCode.InvalidArgument   => FcmErrorType.InvalidToken,
        MessagingErrorCode.SenderIdMismatch  => FcmErrorType.SenderMismatch,
        MessagingErrorCode.ThirdPartyAuthError => FcmErrorType.InvalidAuthConfiguration,
        MessagingErrorCode.Internal          => FcmErrorType.ServiceUnavailable,
        MessagingErrorCode.Unavailable       => FcmErrorType.ServiceUnavailable,
        MessagingErrorCode.QuotaExceeded     => FcmErrorType.ServiceUnavailable,
        _                                    => FcmErrorType.Unknown,
    };

    /// <summary>
    /// True when the token itself is the problem and will never deliver — deactivate it. Per the MO9a spec:
    /// Unregistered / InvalidArgument (dead/malformed token) and SenderIdMismatch (wrong sender project).
    /// </summary>
    public static bool IsTokenInvalid(MessagingErrorCode code)
        => Classify(code) is FcmErrorType.InvalidToken or FcmErrorType.SenderMismatch;

    /// <summary>Convenience over a raised exception (null error code → not a token problem).</summary>
    public static bool IsTokenInvalid(FirebaseMessagingException ex)
        => ex.MessagingErrorCode is { } c && IsTokenInvalid(c);
}
