using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

public sealed class LoginWithUsernameBffCommand : AizenCommand<UserLoginResponse>
{
    public LoginWithUsernameRequest Request { get; }
    public LoginWithUsernameBffCommand(LoginWithUsernameRequest request) { Request = request; }
}
