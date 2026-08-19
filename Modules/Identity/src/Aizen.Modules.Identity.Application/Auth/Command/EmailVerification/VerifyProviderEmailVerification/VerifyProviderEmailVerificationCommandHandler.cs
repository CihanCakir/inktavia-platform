using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.VerifyProviderEmailVerification;

public sealed class VerifyProviderEmailVerificationCommandHandler
    : AizenCommandHandler<VerifyProviderEmailVerificationCommand, VerifyProviderEmailVerificationResponse>
{
    private readonly IProviderEmailVerificationDomainService _service;

    public VerifyProviderEmailVerificationCommandHandler(IProviderEmailVerificationDomainService service)
    {
        _service = service;
    }

    public override async Task<VerifyProviderEmailVerificationResponse?> Handle(
        VerifyProviderEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.VerifyAsync(request.Token, cancellationToken);

        return new VerifyProviderEmailVerificationResponse
        {
            Verified = result.Verified,
            UserId = result.UserId,
            KeycloakSubjectId = result.KeycloakSubjectId,
            Email = result.Email,
            Message = result.Message,
        };
    }
}
