using Aizen.Bff.MarineProvider.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.MarineProvider.Application.Common.Services.PasswordRecovery;

/// <summary>
/// Delivers the recovery OTP to the provider over the requested channel.
/// PLANNED PRODUCTION PATH: MarineProvider BFF → Notification module / message bus → email/SMS.
/// No SMTP/SMS provider credentials live in the BFF or frontend.
/// </summary>
public interface IProviderPasswordRecoveryNotifier
{
    Task SendOtpAsync(string channel, string maskedTarget, string otp, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default stub notifier used until Notification integration ships. It NEVER logs the OTP in a normal
/// configuration; it only records delivery intent. If <c>PasswordRecovery:DevExposeOtp</c> is enabled
/// (local dev only), it logs the code at Debug so the flow can be exercised without a real provider.
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

    public Task SendOtpAsync(string channel, string maskedTarget, string otp, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Password recovery OTP dispatched to {MaskedTarget} via {Channel} (Notification integration pending).",
            maskedTarget, channel);

        if (_options.DevExposeOtp)
        {
            // DEV-ONLY: gated by config, off by default. Do NOT enable outside local development.
            _logger.LogDebug("[DEV-ONLY] Recovery OTP for {MaskedTarget}: {Otp}", maskedTarget, otp);
        }

        return Task.CompletedTask;
    }
}
