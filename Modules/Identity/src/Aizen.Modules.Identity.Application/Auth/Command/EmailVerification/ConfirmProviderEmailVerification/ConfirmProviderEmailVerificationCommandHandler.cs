using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ConfirmProviderEmailVerification;

public sealed class ConfirmProviderEmailVerificationCommandHandler
    : AizenCommandHandler<ConfirmProviderEmailVerificationCommand, ConfirmProviderEmailVerificationResponse>
{
    private readonly IProviderEmailVerificationDomainService _service;

    public ConfirmProviderEmailVerificationCommandHandler(IProviderEmailVerificationDomainService service)
    {
        _service = service;
    }

    public override async Task<ConfirmProviderEmailVerificationResponse?> Handle(
        ConfirmProviderEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ConfirmAsync(request.Token, cancellationToken);

        return new ConfirmProviderEmailVerificationResponse
        {
            Confirmed = result.Confirmed,
            UserId = result.UserId,
            KeycloakSubjectId = result.KeycloakSubjectId,
            Email = result.Email,
            Message = result.Message,
        };
    }
}
