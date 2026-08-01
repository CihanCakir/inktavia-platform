using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.VerifyAdminOtpLogin;

public sealed class VerifyAdminOtpLoginCommand : AizenCommand<VerifyProviderOtpLoginResponse>
{
    public string LoginRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}
