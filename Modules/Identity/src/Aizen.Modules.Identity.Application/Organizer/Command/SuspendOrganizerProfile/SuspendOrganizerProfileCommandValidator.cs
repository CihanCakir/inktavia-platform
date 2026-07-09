using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.SuspendOrganizerProfile;

public sealed class SuspendOrganizerProfileCommandValidator : AizenValidator<SuspendOrganizerProfileCommand>
{
    public SuspendOrganizerProfileCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.ProfileId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
