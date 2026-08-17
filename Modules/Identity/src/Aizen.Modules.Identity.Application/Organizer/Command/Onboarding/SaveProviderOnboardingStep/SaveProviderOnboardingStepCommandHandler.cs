using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.SaveProviderOnboardingStep;

public sealed class SaveProviderOnboardingStepCommandHandler
    : AizenCommandHandler<SaveProviderOnboardingStepCommand, SaveProviderOnboardingStepResponse>
{
    private readonly IProviderOnboardingDomainService _service;
    public SaveProviderOnboardingStepCommandHandler(IProviderOnboardingDomainService service) => _service = service;

    public override async Task<SaveProviderOnboardingStepResponse?> Handle(
        SaveProviderOnboardingStepCommand request, CancellationToken ct)
    {
        await _service.SaveStepAsync(request.ProfileId, request.Step, request.StepStatus, request.StepDataJson, request.SchemaVersion, ct);
        return new SaveProviderOnboardingStepResponse { Success = true, Message = "Step saved." };
    }
}
