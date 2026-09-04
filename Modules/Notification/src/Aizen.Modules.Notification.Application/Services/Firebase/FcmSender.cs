using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Services.Firebase;

/// <summary>
/// BE-MO9a — the real FirebaseAdmin FCM sender that replaces <c>FcmSenderStub</c> (registered only when the Firebase
/// service account is configured; otherwise the stub stays). Adapts the proven legacy
/// <c>FirebasePushNotificationRemoteCall</c> to our <see cref="IFcmSender"/>: it keeps the exact single-token contract
/// the untouched <c>PushNotificationDispatcher</c> already calls, and adds an optional multicast path for batch/region
/// sends. The FirebaseAdmin dependency + message mapping stay inside this adapter — the dispatcher and consumers see
/// only <see cref="IFcmSender"/> and our loose payload, so a later transport swap touches this file alone.
///
/// <para>Invalid-token cleanup: an FCM result whose error is Unregistered / InvalidArgument / SenderIdMismatch means
/// the token will never deliver → the offending <c>UserDeviceTokenEntity</c> is deactivated. A single bad token never
/// fails its siblings in a multicast. Logs counts only — never a token value or the service-account.</para>
/// </summary>
public sealed class FcmSender : IFcmSender
{
    private readonly FirebaseMessaging _messaging;
    private readonly IUserDeviceTokenRepository _tokenRepository;
    private readonly ILogger<FcmSender> _logger;

    public FcmSender(
        IOptions<PushFirebaseSettings> options,
        IUserDeviceTokenRepository tokenRepository,
        ILogger<FcmSender> logger)
    {
        _tokenRepository = tokenRepository;
        _logger = logger;

        var settings = options.Value;
        var app = FirebaseApp.GetInstance(settings.ProjectId)
            ?? FirebaseApp.Create(
                new AppOptions { Credential = GoogleCredential.FromJson(settings.ToServiceAccountJson()) },
                settings.ProjectId);   // named instance → idempotent across scopes/replicas
        _messaging = FirebaseMessaging.GetMessaging(app);
    }

    /// <inheritdoc />
    /// <remarks>Single-token send — the contract the dispatcher already calls. On an invalid-token error the token is
    /// deactivated and a classified <see cref="FcmSendException"/> is thrown (the dispatcher catches per-token, so
    /// siblings still send).</remarks>
    public async Task<string> SendAsync(string deviceToken, string title, string body, string? dataJson, CancellationToken ct)
    {
        var message = FcmMessageMapper.CreateMessage(deviceToken, title, body, dataJson);
        try
        {
            var messageId = await _messaging.SendAsync(message, ct);
            _logger.LogInformation("FCM push sent. Token={Token} MessageId={MessageId}", Mask(deviceToken), messageId);
            return messageId;
        }
        catch (FirebaseMessagingException ex)
        {
            var errorType = FcmErrorClassifier.Classify(ex.MessagingErrorCode ?? default);
            var deactivated = false;

            if (FcmErrorClassifier.IsTokenInvalid(ex))
            {
                await _tokenRepository.DeactivateAsync(deviceToken, ct);
                deactivated = true;
                _logger.LogInformation("FCM token deactivated ({ErrorType}). Token={Token}", errorType, Mask(deviceToken));
            }
            else
            {
                _logger.LogWarning("FCM push failed ({ErrorType}). Token={Token}", errorType, Mask(deviceToken));
            }

            throw new FcmSendException($"FCM send failed: {errorType}.", errorType, deactivated, ex);
        }
    }

    /// <summary>
    /// Batch/region send — chunks the tokens into ≤500-token multicasts, sends each, deactivates every invalid token,
    /// and never throws on a single bad token. Returns the aggregate outcome. Not on <see cref="IFcmSender"/> so the
    /// untouched per-token dispatcher path is unaffected; available for future bulk fan-out.
    /// </summary>
    public async Task<FcmMulticastResult> SendMulticastAsync(
        IReadOnlyList<string> deviceTokens, string title, string body, string? dataJson, CancellationToken ct)
    {
        if (deviceTokens is null || deviceTokens.Count == 0) return FcmMulticastResult.Empty;

        int success = 0, failed = 0, deactivated = 0;

        foreach (var chunk in FcmMessageMapper.ChunkTokens(deviceTokens))
        {
            var multicast = FcmMessageMapper.CreateMulticast(chunk, title, body, dataJson);
            var response = await _messaging.SendEachForMulticastAsync(multicast, ct);

            for (var i = 0; i < response.Responses.Count; i++)
            {
                var r = response.Responses[i];
                if (r.IsSuccess) { success++; continue; }

                failed++;
                var token = chunk[i];
                if (r.Exception is { } ex && FcmErrorClassifier.IsTokenInvalid(ex))
                {
                    await _tokenRepository.DeactivateAsync(token, ct);
                    deactivated++;
                }
            }
        }

        _logger.LogInformation(
            "FCM multicast complete. Sent={Success} Failed={Failed} Deactivated={Deactivated} Tokens={Total}",
            success, failed, deactivated, deviceTokens.Count);

        return new FcmMulticastResult(success, failed, deactivated);
    }

    /// <summary>Log-safe token — a short prefix only; a real token value is never logged.</summary>
    private static string Mask(string token)
        => string.IsNullOrEmpty(token) ? "(empty)" : token[..Math.Min(8, token.Length)] + "…";
}
