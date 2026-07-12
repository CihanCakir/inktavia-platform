using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;

internal sealed class LoggingProviderOtpLoginNotifier : IProviderOtpLoginNotifier
{
    private readonly PasswordRecoveryOptions _pwdOptions;
    private readonly ILogger<LoggingProviderOtpLoginNotifier> _logger;

    public LoggingProviderOtpLoginNotifier(
        IOptions<PasswordRecoveryOptions> pwdOptions,
        ILogger<LoggingProviderOtpLoginNotifier> logger)
    {
        _pwdOptions = pwdOptions.Value;
        _logger = logger;
    }

    public Task SendOtpAsync(string channel, string maskedTarget, string otp,
        long recipientUserId, string? email, string? phone, int expiresInMinutes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OTP login code dispatched to {MaskedTarget} via {Channel} (Logging mode).", maskedTarget, channel);
        if (_pwdOptions.DevExposeOtp)
            _logger.LogDebug("[DEV-ONLY] OTP login code for {MaskedTarget}: {Otp}", maskedTarget, otp);
        return Task.CompletedTask;
    }
}
