using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendProviderOtpLogin;

public sealed class ResendProviderOtpLoginCommand : AizenCommand<ResendProviderOtpLoginResponse>
{
    public string LoginRequestId { get; set; } = default!;
}
