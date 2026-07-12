using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.SubmitProviderOnboarding;

public sealed class SubmitProviderOnboardingCommandValidator : AizenValidator<SubmitProviderOnboardingCommand>
{
    public SubmitProviderOnboardingCommandValidator()
    {
        RuleFor(x => x.ProfileId).GreaterThan(0);
    }
}
