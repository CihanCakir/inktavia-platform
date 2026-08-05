using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant.ValidateParticipantSocial;

public sealed class ValidateParticipantSocialCommandValidator
    : AizenValidator<ValidateParticipantSocialCommand>
{
    public ValidateParticipantSocialCommandValidator()
    {
        RuleFor(x => x.Provider).NotEmpty();
        RuleFor(x => x.IdToken).NotEmpty();
    }
}
