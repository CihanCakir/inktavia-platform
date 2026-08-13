using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.UpdateContentItem;

public sealed class UpdateContentItemCommandValidator : AbstractValidator<UpdateContentItemCommand>
{
    public UpdateContentItemCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();

        RuleFor(x => x.DefaultLanguage)
            .Length(2, 10)
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultLanguage));
    }
}
