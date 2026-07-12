using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.SubmitProviderOnboarding;

public sealed class SubmitProviderOnboardingCommandHandler
    : AizenCommandHandler<SubmitProviderOnboardingCommand, SubmitProviderOnboardingResponse>
{
    private readonly IProviderOnboardingDomainService _service;
    public SubmitProviderOnboardingCommandHandler(IProviderOnboardingDomainService service) => _service = service;

    public override async Task<SubmitProviderOnboardingResponse?> Handle(
        SubmitProviderOnboardingCommand request, CancellationToken ct)
    {
        await _service.SubmitAsync(request.ProfileId, ct);
        return new SubmitProviderOnboardingResponse { Success = true, Message = "Onboarding submitted for review." };
    }
}
