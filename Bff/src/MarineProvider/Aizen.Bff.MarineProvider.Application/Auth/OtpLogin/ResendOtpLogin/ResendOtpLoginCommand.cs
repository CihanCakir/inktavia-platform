using Aizen.Bff.MarineProvider.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth.OtpLogin.ResendOtpLogin;

public sealed class ResendOtpLoginCommand : AizenCommand<OtpLoginResendResponse>
{
    public string LoginRequestId { get; set; } = default!;
}
