using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

public sealed class ChangePasswordBffCommand : AizenCommand<ChangePasswordDto>
{
    public ChangePasswordRequest Request { get; }
    public ChangePasswordBffCommand(ChangePasswordRequest request)
    {
        Request = request;
    }
}
