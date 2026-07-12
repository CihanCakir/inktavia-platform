using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Onboarding.SaveOnboardingStep;

public sealed class SaveOnboardingStepCommand : AizenCommand<SaveOnboardingStepResponse>
{
    public string Step { get; set; } = default!;
    public string StepStatus { get; set; } = default!;
    public System.Text.Json.JsonElement StepData { get; set; }
    public int SchemaVersion { get; set; } = 1;
}
