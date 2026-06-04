using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

public sealed class LoginWithUsernameCommand : AizenCommand<UserLoginResponse>
{
    public LoginWithUsernameRequest Request { get; }
    public LoginWithUsernameCommand(LoginWithUsernameRequest request) { Request = request; }
}
