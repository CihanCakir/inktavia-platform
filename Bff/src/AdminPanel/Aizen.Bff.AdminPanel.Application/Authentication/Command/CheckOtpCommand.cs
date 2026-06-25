using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

public sealed class CheckOtpCommand : AizenCommand<CheckOtpDto>
{
    public CheckOtpRequest Request { get; }
    public CheckOtpCommand(CheckOtpRequest request) { Request = request; }
}
