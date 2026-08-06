using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

public sealed class CheckOtpBffCommand : AizenCommand<CheckOtpDto>
{
    public CheckOtpRequest Request { get; }
    public CheckOtpBffCommand(CheckOtpRequest request) { Request = request; }
}
