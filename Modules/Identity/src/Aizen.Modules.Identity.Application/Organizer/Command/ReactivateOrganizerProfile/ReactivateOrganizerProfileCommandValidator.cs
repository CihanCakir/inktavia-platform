using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.ReactivateOrganizerProfile;

public sealed class ReactivateOrganizerProfileCommandValidator : AizenValidator<ReactivateOrganizerProfileCommand>
{
    public ReactivateOrganizerProfileCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.ProfileId).GreaterThan(0);
    }
}
