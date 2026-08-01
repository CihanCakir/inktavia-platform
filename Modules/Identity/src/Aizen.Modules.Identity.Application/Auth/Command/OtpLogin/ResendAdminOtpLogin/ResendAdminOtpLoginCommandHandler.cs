using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendAdminOtpLogin;

public sealed class ResendAdminOtpLoginCommandHandler
    : AizenCommandHandler<ResendAdminOtpLoginCommand, ResendProviderOtpLoginResponse>
{
    private readonly IAdminOtpLoginDomainService _service;
    public ResendAdminOtpLoginCommandHandler(IAdminOtpLoginDomainService service) => _service = service;

    public override async Task<ResendProviderOtpLoginResponse?> Handle(
        ResendAdminOtpLoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ResendAsync(request.LoginRequestId, cancellationToken);
        return new ResendProviderOtpLoginResponse
        {
            Resent = result.Resent, ResendAfterSeconds = result.ResendAfterSeconds,
            ExpiresInSeconds = result.ExpiresInSeconds,
            Message = result.Message,
        };
    }
}
