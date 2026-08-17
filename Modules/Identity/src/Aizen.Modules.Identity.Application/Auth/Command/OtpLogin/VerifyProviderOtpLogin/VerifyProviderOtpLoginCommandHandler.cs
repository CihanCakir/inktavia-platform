using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.VerifyProviderOtpLogin;

public sealed class VerifyProviderOtpLoginCommandHandler
    : AizenCommandHandler<VerifyProviderOtpLoginCommand, VerifyProviderOtpLoginResponse>
{
    private readonly IProviderOtpLoginDomainService _service;
    public VerifyProviderOtpLoginCommandHandler(IProviderOtpLoginDomainService service) => _service = service;

    public override async Task<VerifyProviderOtpLoginResponse?> Handle(
        VerifyProviderOtpLoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.VerifyOtpAsync(request.LoginRequestId, request.OtpCode, cancellationToken);
        return new VerifyProviderOtpLoginResponse
        {
            Verified = result.Verified, NextAction = result.NextAction,
            AuthorizationUrl = result.AuthorizationUrl,
            LoginTicket = result.LoginTicket,
            ExpiresInSeconds = result.ExpiresInSeconds, Message = result.Message,
        };
    }
}
