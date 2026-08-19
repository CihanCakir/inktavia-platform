using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ConsumeProviderEmailVerification;

public sealed class ConsumeProviderEmailVerificationCommandHandler
    : AizenCommandHandler<ConsumeProviderEmailVerificationCommand, ConsumeProviderEmailVerificationResponse>
{
    private readonly IProviderEmailVerificationDomainService _service;

    public ConsumeProviderEmailVerificationCommandHandler(IProviderEmailVerificationDomainService service)
    {
        _service = service;
    }

    public override async Task<ConsumeProviderEmailVerificationResponse?> Handle(
        ConsumeProviderEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ConsumeAsync(request.Token, cancellationToken);

        return new ConsumeProviderEmailVerificationResponse
        {
            Consumed = result.Consumed,
            Message = result.Message,
        };
    }
}
