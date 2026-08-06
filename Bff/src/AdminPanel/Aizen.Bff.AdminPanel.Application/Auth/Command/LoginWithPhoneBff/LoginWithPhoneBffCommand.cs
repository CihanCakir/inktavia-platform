using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

public sealed class LoginWithPhoneBffCommand : AizenCommand<UserLoginResponse>
{
    public LoginWithPhoneRequest Request { get; }
    public LoginWithPhoneBffCommand(LoginWithPhoneRequest request) { Request = request; }
}
