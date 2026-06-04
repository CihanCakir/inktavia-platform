using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

public sealed class LoginWithPhoneCommand : AizenCommand<UserLoginResponse>
{
    public LoginWithPhoneRequest Request { get; }
    public LoginWithPhoneCommand(LoginWithPhoneRequest request) { Request = request; }
}
