using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

public sealed class LoginWithOtpBffCommand : AizenCommand<UserLoginResponse>
{
    public LoginWithOtpRequest Request { get; }
    public LoginWithOtpBffCommand(LoginWithOtpRequest request) { Request = request; }
}
