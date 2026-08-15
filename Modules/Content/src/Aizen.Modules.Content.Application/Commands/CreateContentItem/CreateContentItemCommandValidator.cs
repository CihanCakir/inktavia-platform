using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.CreateContentItem;

public sealed class CreateContentItemCommandValidator : AbstractValidator<CreateContentItemCommand>
{
    public CreateContentItemCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();

        // W3.4 — a provided slug must not collide with a reserved static route segment (or its localized form).
        RuleFor(x => x.Slug)
            .Must(slug => !ReservedSlugs.IsReserved(slug))
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug is reserved and would be unreachable behind a static route.");

        RuleFor(x => x.DefaultLanguage)
            .NotEmpty().Length(2, 10);

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("At least one translation is required.");

        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Lang).NotEmpty().Length(2, 10);
            t.RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        });

        RuleFor(x => x)
            .Must(HasDefaultLanguageTitle)
            .WithMessage("A translation for the DefaultLanguage with a non-empty Title is required.");

        RuleFor(x => x.AuthorUserId).GreaterThan(0);
    }

    private static bool HasDefaultLanguageTitle(CreateContentItemCommand c)
        => c.Translations.Any(t =>
            string.Equals(t.Lang, c.DefaultLanguage, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(t.Title));
}
