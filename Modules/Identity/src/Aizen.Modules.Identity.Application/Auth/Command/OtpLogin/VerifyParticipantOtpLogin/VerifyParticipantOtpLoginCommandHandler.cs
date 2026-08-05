using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.VerifyParticipantOtpLogin;

public sealed class VerifyParticipantOtpLoginCommandHandler
    : AizenCommandHandler<VerifyParticipantOtpLoginCommand, VerifyProviderOtpLoginResponse>
{
    private readonly IParticipantOtpLoginDomainService _service;
    public VerifyParticipantOtpLoginCommandHandler(IParticipantOtpLoginDomainService service) => _service = service;

    public override async Task<VerifyProviderOtpLoginResponse?> Handle(
        VerifyParticipantOtpLoginCommand request, CancellationToken cancellationToken)
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
