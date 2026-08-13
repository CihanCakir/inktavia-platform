using FluentValidation;

namespace Aizen.Modules.Content.Application.Queries.GetPublicContentBySlug;

public sealed class GetPublicContentBySlugQueryValidator : AbstractValidator<GetPublicContentBySlugQuery>
{
    public GetPublicContentBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
    }
}
