using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

public sealed class RefreshBffCommand : AizenCommand<UserLoginResponse>
{
    public RefreshLoginHttpRequest Request { get; }
    public RefreshBffCommand(RefreshLoginHttpRequest request) { Request = request; }
}
