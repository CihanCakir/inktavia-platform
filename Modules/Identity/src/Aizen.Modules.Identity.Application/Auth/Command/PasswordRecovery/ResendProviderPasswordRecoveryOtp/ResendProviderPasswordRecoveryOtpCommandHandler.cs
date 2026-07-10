using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResendProviderPasswordRecoveryOtp;

public sealed class ResendProviderPasswordRecoveryOtpCommandHandler
    : AizenCommandHandler<ResendProviderPasswordRecoveryOtpCommand, ResendProviderPasswordRecoveryOtpResponse>
{
    private readonly IProviderPasswordRecoveryDomainService _service;

    public ResendProviderPasswordRecoveryOtpCommandHandler(IProviderPasswordRecoveryDomainService service)
    {
        _service = service;
    }

    public override async Task<ResendProviderPasswordRecoveryOtpResponse?> Handle(
        ResendProviderPasswordRecoveryOtpCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ResendAsync(request.ResetRequestId, cancellationToken);

        return new ResendProviderPasswordRecoveryOtpResponse
        {
            Resent = result.Resent,
            ResendAfterSeconds = result.ResendAfterSeconds,
            ExpiresInSeconds = result.ExpiresInSeconds,
            Message = "If an account exists, a new verification code has been sent.",
        };
    }
}
