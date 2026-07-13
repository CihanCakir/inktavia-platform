using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Onboarding.SaveOnboardingStep;

public sealed class SaveOnboardingStepCommand : AizenCommand<SaveOnboardingStepResponse>
{
    public string Step { get; set; } = default!;
    public string StepStatus { get; set; } = default!;
    /// <summary>The step's answers as raw JSON. A string is the only shape that survives both
    /// Newtonsoft (MVC binding) and System.Text.Json (Refit).</summary>
    public string StepDataJson { get; set; } = default!;
    public int SchemaVersion { get; set; } = 1;
}
