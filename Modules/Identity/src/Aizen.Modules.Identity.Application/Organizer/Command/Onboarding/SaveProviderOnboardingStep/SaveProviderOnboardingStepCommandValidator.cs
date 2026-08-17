using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.SaveProviderOnboardingStep;

public sealed class SaveProviderOnboardingStepCommandValidator : AizenValidator<SaveProviderOnboardingStepCommand>
{
    private static readonly string[] ValidSteps = { "BusinessIdentity", "ServiceCapabilities", "OperatingRegion", "ComplianceVerification", "CargoDryInterest", "ReviewSubmit" };

    public SaveProviderOnboardingStepCommandValidator()
    {
        RuleFor(x => x.ProfileId).GreaterThan(0);
        RuleFor(x => x.Step).NotEmpty().Must(s => ValidSteps.Contains(s)).WithMessage("Unknown onboarding step.");
        RuleFor(x => x.StepStatus).NotEmpty();
        RuleFor(x => x.StepDataJson).NotEmpty();
    }
}
