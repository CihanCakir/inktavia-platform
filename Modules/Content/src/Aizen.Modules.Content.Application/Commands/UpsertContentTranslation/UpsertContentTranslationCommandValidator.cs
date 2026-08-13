using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.UpsertContentTranslation;

public sealed class UpsertContentTranslationCommandValidator : AbstractValidator<UpsertContentTranslationCommand>
{
    public UpsertContentTranslationCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.Lang).NotEmpty().Length(2, 10);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
    }
}
