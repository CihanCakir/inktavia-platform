using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ResendProviderEmailVerification;

public sealed class ResendProviderEmailVerificationCommandHandler
    : AizenCommandHandler<ResendProviderEmailVerificationCommand, ResendProviderEmailVerificationResponse>
{
    private readonly IProviderEmailVerificationDomainService _service;

    public ResendProviderEmailVerificationCommandHandler(IProviderEmailVerificationDomainService service)
    {
        _service = service;
    }

    public override async Task<ResendProviderEmailVerificationResponse?> Handle(
        ResendProviderEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ResendAsync(request.Email, cancellationToken);

        return new ResendProviderEmailVerificationResponse
        {
            Accepted = result.Accepted,
            MaskedTarget = result.MaskedTarget,
            ResendAfterSeconds = result.ResendAfterSeconds,
            ExpiresInSeconds = result.ExpiresInSeconds,
            // Numaralandırma korumalı jenerik mesaj.
            Message = "Hesabınız varsa ve e-postanız henüz doğrulanmadıysa yeni bir doğrulama bağlantısı gönderildi.",
        };
    }
}
