using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendProviderOtpLogin;

public sealed class ResendProviderOtpLoginCommandHandler
    : AizenCommandHandler<ResendProviderOtpLoginCommand, ResendProviderOtpLoginResponse>
{
    private readonly IProviderOtpLoginDomainService _service;
    public ResendProviderOtpLoginCommandHandler(IProviderOtpLoginDomainService service) => _service = service;

    public override async Task<ResendProviderOtpLoginResponse?> Handle(
        ResendProviderOtpLoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ResendAsync(request.LoginRequestId, cancellationToken);
        return new ResendProviderOtpLoginResponse
        {
            Resent = result.Resent, ResendAfterSeconds = result.ResendAfterSeconds,
            ExpiresInSeconds = result.ExpiresInSeconds,
            // Pass the domain message through: when the cooldown blocks a resend, Resent=false and the
            // message says so, instead of falsely claiming a new code was sent.
            Message = result.Message,
        };
    }
}
