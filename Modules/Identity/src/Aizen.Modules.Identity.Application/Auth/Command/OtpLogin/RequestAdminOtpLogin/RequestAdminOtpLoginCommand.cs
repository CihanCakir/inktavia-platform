using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.RequestAdminOtpLogin;

public sealed class RequestAdminOtpLoginCommand : AizenCommand<RequestProviderOtpLoginResponse>
{
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}
