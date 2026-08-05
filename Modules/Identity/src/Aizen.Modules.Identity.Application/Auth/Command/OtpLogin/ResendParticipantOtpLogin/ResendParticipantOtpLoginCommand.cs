using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendParticipantOtpLogin;

public sealed class ResendParticipantOtpLoginCommand : AizenCommand<ResendProviderOtpLoginResponse>
{
    public string LoginRequestId { get; set; } = default!;
}
