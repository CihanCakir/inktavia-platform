namespace Aizen.Modules.Identity.Domain.Interface.Service;

/// <summary>
/// Doğrulama linkini teslim hattına (Notification modülü) iletir. Token içeren <paramref name="verifyUrl"/>
/// yalnızca iç kanalda dolaşır.
/// </summary>
public interface IProviderEmailVerificationNotifier
{
    Task SendVerificationAsync(
        long recipientUserId,
        string email,
        string maskedTarget,
        string verifyUrl,
        int expiresInMinutes,
        CancellationToken cancellationToken = default);
}
