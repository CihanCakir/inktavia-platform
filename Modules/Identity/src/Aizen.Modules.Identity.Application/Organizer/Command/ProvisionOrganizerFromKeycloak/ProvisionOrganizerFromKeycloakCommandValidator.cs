using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.ProvisionOrganizerFromKeycloak;

public sealed class ProvisionOrganizerFromKeycloakCommandValidator
    : AizenValidator<ProvisionOrganizerFromKeycloakCommand>
{
    public ProvisionOrganizerFromKeycloakCommandValidator()
    {
        RuleFor(x => x.KeycloakSubjectId).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).MaximumLength(80).When(x => !string.IsNullOrWhiteSpace(x.FirstName));
        RuleFor(x => x.LastName).MaximumLength(80).When(x => !string.IsNullOrWhiteSpace(x.LastName));
        RuleFor(x => x.CompanyName).MaximumLength(300).When(x => !string.IsNullOrWhiteSpace(x.CompanyName));
    }
}
