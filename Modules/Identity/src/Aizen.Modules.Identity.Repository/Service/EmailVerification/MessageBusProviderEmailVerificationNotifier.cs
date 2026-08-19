using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;

/// <summary>
/// Doğrulama linkini, Notification modülünce e-postaya çevrilmek üzere iç mesaj kuyruğuna yayınlar.
/// <see cref="ProviderEmailVerificationOptions.DevExposeVerifyUrl"/> bayrağı ayrıca linki Debug seviyesinde
/// loglar (yerel geliştirme). Token içeren ham link yalnızca iç kuyrukta dolaşır — BFF'e/tarayıcıya asla gitmez.
/// </summary>
internal sealed class MessageBusProviderEmailVerificationNotifier : IProviderEmailVerificationNotifier
{
    private readonly IAizenMessagePublisher _publisher;
    private readonly ProviderEmailVerificationOptions _options;
    private readonly ILogger<MessageBusProviderEmailVerificationNotifier> _logger;

    public MessageBusProviderEmailVerificationNotifier(
        IAizenMessagePublisher publisher,
        IOptions<ProviderEmailVerificationOptions> options,
        ILogger<MessageBusProviderEmailVerificationNotifier> logger)
    {
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationAsync(
        long recipientUserId,
        string email,
        string maskedTarget,
        string verifyUrl,
        int expiresInMinutes,
        CancellationToken cancellationToken = default)
    {
        var message = new ProviderEmailVerificationRequestedMessage
        {
            RecipientUserId = recipientUserId,
            Email = email,
            MaskedTarget = maskedTarget,
            VerifyUrl = verifyUrl,
            ExpiresInMinutes = expiresInMinutes,
        };

        await _publisher.PublishAsync(message, cancellationToken);

        _logger.LogInformation(
            "Provider email verification published to message bus for {MaskedTarget}.", maskedTarget);

        if (_options.DevExposeVerifyUrl)
        {
            _logger.LogDebug("[DEV-ONLY] Email verification link for {MaskedTarget}: {VerifyUrl}", maskedTarget, verifyUrl);
        }
    }
}
