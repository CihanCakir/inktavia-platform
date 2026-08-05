using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendParticipantOtpLogin;

public sealed class ResendParticipantOtpLoginCommandHandler
    : AizenCommandHandler<ResendParticipantOtpLoginCommand, ResendProviderOtpLoginResponse>
{
    private readonly IParticipantOtpLoginDomainService _service;
    public ResendParticipantOtpLoginCommandHandler(IParticipantOtpLoginDomainService service) => _service = service;

    public override async Task<ResendProviderOtpLoginResponse?> Handle(
        ResendParticipantOtpLoginCommand request, CancellationToken cancellationToken)
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
