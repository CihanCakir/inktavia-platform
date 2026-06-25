using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

public sealed class ChangePasswordCommand : AizenCommand<ChangePasswordDto>
{
    public ChangePasswordRequest Request { get; }
    public string UserToken { get; }
    public ChangePasswordCommand(ChangePasswordRequest request, string userToken)
    {
        Request = request;
        UserToken = userToken;
    }
}
