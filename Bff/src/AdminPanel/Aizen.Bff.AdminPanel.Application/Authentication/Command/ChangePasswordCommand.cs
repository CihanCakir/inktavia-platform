using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

public sealed class ChangePasswordCommand : AizenCommand<ChangePasswordDto>
{
    public ChangePasswordRequest Request { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public ChangePasswordCommand(ChangePasswordRequest request, string authorization, string userToken)
    {
        Request = request;
        Authorization = authorization;
        UserToken = userToken;
    }
}
