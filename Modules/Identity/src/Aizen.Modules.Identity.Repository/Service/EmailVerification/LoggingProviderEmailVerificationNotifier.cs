using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;

/// <summary>
/// Teslim niyetini loglayan stub notifier. <c>ProviderEmailVerification:DeliveryMode</c> = <c>Logging</c>
/// olduğunda (yerel geliştirme) kullanılır. <see cref="ProviderEmailVerificationOptions.DevExposeVerifyUrl"/>
/// bayrağı, doğrulama linkini Debug seviyesinde loglamayı açar. Paylaşımlı/test/prod'da MUTLAKA false kalmalı.
/// </summary>
internal sealed class LoggingProviderEmailVerificationNotifier : IProviderEmailVerificationNotifier
{
    private readonly ProviderEmailVerificationOptions _options;
    private readonly ILogger<LoggingProviderEmailVerificationNotifier> _logger;

    public LoggingProviderEmailVerificationNotifier(
        IOptions<ProviderEmailVerificationOptions> options,
        ILogger<LoggingProviderEmailVerificationNotifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendVerificationAsync(
        long recipientUserId,
        string email,
        string maskedTarget,
        string verifyUrl,
        int expiresInMinutes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Provider email verification dispatched to {MaskedTarget} (Logging mode — no real delivery).",
            maskedTarget);

        if (_options.DevExposeVerifyUrl)
        {
            _logger.LogDebug("[DEV-ONLY] Email verification link for {MaskedTarget}: {VerifyUrl}", maskedTarget, verifyUrl);
        }

        return Task.CompletedTask;
    }
}
