using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command;

public class ChangePasswordCommand : AizenCommand<ChangePasswordDto>
{
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? NewPasswordConfirm { get; set; }
    public ChangePasswordCommand(string? oldPassword, string? newPassword, string? newPasswordConfirm)
    {
        OldPassword = oldPassword;
        NewPassword = newPassword;
        NewPasswordConfirm = newPasswordConfirm;
    }
}
