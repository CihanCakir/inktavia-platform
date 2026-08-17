using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.SaveProviderOnboardingStep;

public sealed class SaveProviderOnboardingStepCommand : AizenCommand<SaveProviderOnboardingStepResponse>
{
    public long ProfileId { get; set; }
    public string Step { get; set; } = default!;
    public string StepStatus { get; set; } = default!;
    public string StepDataJson { get; set; } = default!;
    public int SchemaVersion { get; set; } = 1;
}
