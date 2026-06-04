using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

public sealed class SendOtpCommand : AizenCommand<SendOtpDto>
{
    public SendOtpRequest Request { get; }
    public SendOtpCommand(SendOtpRequest request) { Request = request; }
}
