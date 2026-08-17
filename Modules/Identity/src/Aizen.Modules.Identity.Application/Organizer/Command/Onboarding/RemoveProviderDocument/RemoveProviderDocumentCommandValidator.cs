using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.RemoveProviderDocument;

public sealed class RemoveProviderDocumentCommandValidator : AizenValidator<RemoveProviderDocumentCommand>
{
    public RemoveProviderDocumentCommandValidator()
    {
        RuleFor(x => x.ProfileId).GreaterThan(0);
        RuleFor(x => x.FileId).NotEmpty().WithMessage("FileId is required.");
    }
}
