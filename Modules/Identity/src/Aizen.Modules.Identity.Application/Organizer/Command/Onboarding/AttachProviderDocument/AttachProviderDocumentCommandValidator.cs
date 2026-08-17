using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.AttachProviderDocument;

public sealed class AttachProviderDocumentCommandValidator : AizenValidator<AttachProviderDocumentCommand>
{
    public AttachProviderDocumentCommandValidator()
    {
        RuleFor(x => x.ProfileId).GreaterThan(0);
        RuleFor(x => x.FileId).NotEmpty().WithMessage("FileId is required.");
        RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(100);
    }
}
