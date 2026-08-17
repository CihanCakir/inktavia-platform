using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;

internal sealed class MessageBusProviderOtpLoginNotifier : IProviderOtpLoginNotifier
{
    private readonly IAizenMessagePublisher _publisher;
    private readonly PasswordRecoveryOptions _pwdOptions;
    private readonly ILogger<MessageBusProviderOtpLoginNotifier> _logger;

    public MessageBusProviderOtpLoginNotifier(
        IAizenMessagePublisher publisher,
        IOptions<PasswordRecoveryOptions> pwdOptions,
        ILogger<MessageBusProviderOtpLoginNotifier> logger)
    {
        _publisher = publisher;
        _pwdOptions = pwdOptions.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(string channel, string maskedTarget, string otp,
        long recipientUserId, string? email, string? phone, int expiresInMinutes,
        CancellationToken cancellationToken = default)
    {
        await _publisher.PublishAsync(new ProviderOtpLoginOtpRequestedMessage
        {
            RecipientUserId = recipientUserId, Channel = channel, MaskedTarget = maskedTarget,
            Otp = otp, Email = email, Phone = phone, ExpiresInMinutes = expiresInMinutes,
        }, cancellationToken);

        _logger.LogInformation("OTP login code published to message bus for {MaskedTarget} via {Channel}.", maskedTarget, channel);
        if (_pwdOptions.DevExposeOtp)
            _logger.LogDebug("[DEV-ONLY] OTP login code for {MaskedTarget}: {Otp}", maskedTarget, otp);
    }
}
