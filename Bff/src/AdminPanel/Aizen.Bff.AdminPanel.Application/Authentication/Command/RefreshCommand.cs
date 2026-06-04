using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

public sealed class RefreshCommand : AizenCommand<UserLoginResponse>
{
    public RefreshLoginHttpRequest Request { get; }
    public RefreshCommand(RefreshLoginHttpRequest request) { Request = request; }
}
