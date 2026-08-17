using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.CreateContentCategory;

public sealed class CreateContentCategoryCommandValidator : AbstractValidator<CreateContentCategoryCommand>
{
    public CreateContentCategoryCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Name).NotEmpty().WithMessage("At least one localized name is required.");
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
    }
}
