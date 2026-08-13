using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.UpdateContentCategory;

public sealed class UpdateContentCategoryCommandValidator : AbstractValidator<UpdateContentCategoryCommand>
{
    public UpdateContentCategoryCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("At least one localized name is required.");
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
    }
}
