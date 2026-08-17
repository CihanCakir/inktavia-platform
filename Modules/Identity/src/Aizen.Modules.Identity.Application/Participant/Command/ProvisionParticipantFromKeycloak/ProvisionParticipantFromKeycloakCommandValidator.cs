using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant.ProvisionParticipantFromKeycloak;

public sealed class ProvisionParticipantFromKeycloakCommandValidator
    : AizenValidator<ProvisionParticipantFromKeycloakCommand>
{
    public ProvisionParticipantFromKeycloakCommandValidator()
    {
        RuleFor(x => x.KeycloakSubjectId).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).MaximumLength(80).When(x => !string.IsNullOrWhiteSpace(x.FirstName));
        RuleFor(x => x.LastName).MaximumLength(80).When(x => !string.IsNullOrWhiteSpace(x.LastName));
    }
}
