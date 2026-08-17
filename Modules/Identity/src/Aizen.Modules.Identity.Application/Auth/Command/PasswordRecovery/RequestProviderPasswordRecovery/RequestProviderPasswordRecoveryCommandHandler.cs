using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.RequestProviderPasswordRecovery;

public sealed class RequestProviderPasswordRecoveryCommandHandler
    : AizenCommandHandler<RequestProviderPasswordRecoveryCommand, RequestProviderPasswordRecoveryResponse>
{
    private readonly IProviderPasswordRecoveryDomainService _service;

    public RequestProviderPasswordRecoveryCommandHandler(IProviderPasswordRecoveryDomainService service)
    {
        _service = service;
    }

    public override async Task<RequestProviderPasswordRecoveryResponse?> Handle(
        RequestProviderPasswordRecoveryCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.RequestAsync(request.Channel, request.Identifier, cancellationToken);

        return new RequestProviderPasswordRecoveryResponse
        {
            Accepted = true,
            ResetRequestId = result.ResetRequestId,
            MaskedTarget = result.MaskedTarget,
            OtpLength = result.OtpLength,
            ExpiresInSeconds = result.ExpiresInSeconds,
            ResendAfterSeconds = result.ResendAfterSeconds,
            Message = "If an account exists, a verification code has been sent.",
        };
    }
}
