using Aizen.Modules.Content.Application.Services;
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

        // W3.4 — a re-slug must not collide with a reserved static route segment (or its localized form).
        RuleFor(x => x.Slug)
            .Must(slug => !ReservedSlugs.IsReserved(slug))
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug is reserved and would be unreachable behind a static route.");
    }
}
