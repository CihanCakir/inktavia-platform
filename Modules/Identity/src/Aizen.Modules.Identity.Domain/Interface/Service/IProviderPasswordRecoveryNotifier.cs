namespace Aizen.Modules.Identity.Domain.Interface.Service;

public interface IProviderPasswordRecoveryNotifier
{
    Task SendOtpAsync(
        string channel,
        string maskedTarget,
        string otp,
        long recipientUserId,
        string? email,
        string? phone,
        int expiresInMinutes,
        CancellationToken cancellationToken = default);
}
