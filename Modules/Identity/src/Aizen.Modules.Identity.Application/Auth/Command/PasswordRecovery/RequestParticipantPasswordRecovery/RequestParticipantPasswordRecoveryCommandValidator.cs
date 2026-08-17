using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.RequestParticipantPasswordRecovery;

public sealed class RequestParticipantPasswordRecoveryCommandValidator
    : AizenValidator<RequestParticipantPasswordRecoveryCommand>
{
    public RequestParticipantPasswordRecoveryCommandValidator()
    {
        RuleFor(x => x.Channel).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(256);
    }
}
