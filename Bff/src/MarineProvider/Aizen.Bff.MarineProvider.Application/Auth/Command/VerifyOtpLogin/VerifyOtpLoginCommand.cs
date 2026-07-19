using Aizen.Bff.MarineProvider.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth;

public sealed class VerifyOtpLoginCommand : AizenCommand<OtpLoginVerifyResponse>
{
    public string LoginRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}
