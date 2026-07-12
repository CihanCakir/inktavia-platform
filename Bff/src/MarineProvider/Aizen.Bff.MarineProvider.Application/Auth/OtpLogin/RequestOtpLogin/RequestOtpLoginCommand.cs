using Aizen.Bff.MarineProvider.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth.OtpLogin.RequestOtpLogin;

public sealed class RequestOtpLoginCommand : AizenCommand<OtpLoginRequestResponse>
{
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}
