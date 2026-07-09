using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.MarkOrganizerPhoneVerified;

public sealed class MarkOrganizerPhoneVerifiedCommandValidator
    : AizenValidator<MarkOrganizerPhoneVerifiedCommand>
{
    public MarkOrganizerPhoneVerifiedCommandValidator()
    {
        RuleFor(x => x.ProfileId).GreaterThan(0);
    }
}
