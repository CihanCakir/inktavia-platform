using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.RequestAdminOtpLogin;

public sealed class RequestAdminOtpLoginCommandHandler
    : AizenCommandHandler<RequestAdminOtpLoginCommand, RequestProviderOtpLoginResponse>
{
    private readonly IAdminOtpLoginDomainService _service;
    public RequestAdminOtpLoginCommandHandler(IAdminOtpLoginDomainService service) => _service = service;

    public override async Task<RequestProviderOtpLoginResponse?> Handle(
        RequestAdminOtpLoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.RequestAsync(request.Channel, request.Identifier, cancellationToken);
        return new RequestProviderOtpLoginResponse
        {
            Accepted = true, LoginRequestId = result.LoginRequestId,
            MaskedTarget = result.MaskedTarget, OtpLength = result.OtpLength,
            ExpiresInSeconds = result.ExpiresInSeconds, ResendAfterSeconds = result.ResendAfterSeconds,
            Message = "If an account exists, a verification code has been sent.",
        };
    }
}
