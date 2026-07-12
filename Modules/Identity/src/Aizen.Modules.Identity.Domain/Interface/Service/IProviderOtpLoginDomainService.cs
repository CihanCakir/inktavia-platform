using Aizen.Modules.Identity.Domain.Model.OtpLogin;

namespace Aizen.Modules.Identity.Domain.Interface.Service;

public interface IProviderOtpLoginDomainService
{
    Task<OtpLoginRequestResult> RequestAsync(string channel, string identifier, CancellationToken ct);
    Task<OtpLoginVerifyResult> VerifyOtpAsync(string loginRequestId, string otpCode, CancellationToken ct);
    Task<OtpLoginResendResult> ResendAsync(string loginRequestId, CancellationToken ct);
}
