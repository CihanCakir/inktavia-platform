using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

public sealed class LoginWithOtpCommand : AizenCommand<UserLoginResponse>
{
    public LoginWithOtpRequest Request { get; }
    public LoginWithOtpCommand(LoginWithOtpRequest request) { Request = request; }
}
