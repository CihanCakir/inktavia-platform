using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.RequestProviderOnboardingRevision;

public sealed class RequestProviderOnboardingRevisionCommandValidator : AizenValidator<RequestProviderOnboardingRevisionCommand>
{
    public RequestProviderOnboardingRevisionCommandValidator()
    {
        RuleFor(x => x.ProfileId).GreaterThan(0);
        RuleFor(x => x.Steps).NotEmpty().Must(s => s.Length > 0).WithMessage("At least one step is required.");
        RuleFor(x => x.Note).NotEmpty();
    }
}
