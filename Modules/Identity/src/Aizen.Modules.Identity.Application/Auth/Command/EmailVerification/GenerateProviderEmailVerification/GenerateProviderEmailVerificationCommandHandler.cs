using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.GenerateProviderEmailVerification;

public sealed class GenerateProviderEmailVerificationCommandHandler
    : AizenCommandHandler<GenerateProviderEmailVerificationCommand, GenerateProviderEmailVerificationResponse>
{
    private readonly IProviderEmailVerificationDomainService _service;

    public GenerateProviderEmailVerificationCommandHandler(IProviderEmailVerificationDomainService service)
    {
        _service = service;
    }

    public override async Task<GenerateProviderEmailVerificationResponse?> Handle(
        GenerateProviderEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.GenerateAsync(request.Email, cancellationToken);

        return new GenerateProviderEmailVerificationResponse
        {
            Accepted = result.Accepted,
            MaskedTarget = result.MaskedTarget,
            ExpiresInSeconds = result.ExpiresInSeconds,
            ResendAfterSeconds = result.ResendAfterSeconds,
            Message = "Doğrulama e-postası gönderildi.",
        };
    }
}
