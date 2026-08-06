using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

public sealed class SendOtpBffCommand : AizenCommand<SendOtpDto>
{
    public SendOtpRequest Request { get; }
    public SendOtpBffCommand(SendOtpRequest request) { Request = request; }
}
