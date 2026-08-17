using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;

/// <summary>
/// Publishes the recovery OTP to the internal message bus for delivery via the Notification module.
/// The <see cref="PasswordRecoveryOptions.DevExposeOtp"/> flag additionally logs the OTP at Debug
/// level for local development. The raw OTP travels only on the internal bus — never to the BFF/browser.
/// </summary>
internal sealed class MessageBusProviderPasswordRecoveryNotifier : IProviderPasswordRecoveryNotifier
{
    private readonly IAizenMessagePublisher _publisher;
    private readonly PasswordRecoveryOptions _options;
    private readonly ILogger<MessageBusProviderPasswordRecoveryNotifier> _logger;

    public MessageBusProviderPasswordRecoveryNotifier(
        IAizenMessagePublisher publisher,
        IOptions<PasswordRecoveryOptions> options,
        ILogger<MessageBusProviderPasswordRecoveryNotifier> logger)
    {
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(
        string channel,
        string maskedTarget,
        string otp,
        long recipientUserId,
        string? email,
        string? phone,
        int expiresInMinutes,
        CancellationToken cancellationToken = default)
    {
        var message = new ProviderPasswordRecoveryOtpRequestedMessage
        {
            RecipientUserId = recipientUserId,
            Channel = channel,
            MaskedTarget = maskedTarget,
            Otp = otp,
            Email = email,
            Phone = phone,
            ExpiresInMinutes = expiresInMinutes,
        };

        await _publisher.PublishAsync(message, cancellationToken);

        _logger.LogInformation(
            "Password recovery OTP published to message bus for {MaskedTarget} via {Channel}.",
            maskedTarget, channel);

        if (_options.DevExposeOtp)
        {
            _logger.LogDebug("[DEV-ONLY] Recovery OTP for {MaskedTarget}: {Otp}", maskedTarget, otp);
        }
    }
}
