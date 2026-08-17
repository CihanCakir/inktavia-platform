using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;

/// <summary>
/// Stub notifier that logs OTP delivery intent. Used when <c>PasswordRecovery:DeliveryMode</c> is
/// <c>Logging</c> (local development). The <see cref="PasswordRecoveryOptions.DevExposeOtp"/> flag
/// gates logging the actual code at Debug level. MUST remain false in shared/test/production.
/// </summary>
internal sealed class LoggingProviderPasswordRecoveryNotifier : IProviderPasswordRecoveryNotifier
{
    private readonly PasswordRecoveryOptions _options;
    private readonly ILogger<LoggingProviderPasswordRecoveryNotifier> _logger;

    public LoggingProviderPasswordRecoveryNotifier(
        IOptions<PasswordRecoveryOptions> options,
        ILogger<LoggingProviderPasswordRecoveryNotifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendOtpAsync(
        string channel,
        string maskedTarget,
        string otp,
        long recipientUserId,
        string? email,
        string? phone,
        int expiresInMinutes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Password recovery OTP dispatched to {MaskedTarget} via {Channel} (Logging mode — no real delivery).",
            maskedTarget, channel);

        if (_options.DevExposeOtp)
        {
            _logger.LogDebug("[DEV-ONLY] Recovery OTP for {MaskedTarget}: {Otp}", maskedTarget, otp);
        }

        return Task.CompletedTask;
    }
}
