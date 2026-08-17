using FluentValidation;

namespace Aizen.Modules.Content.Application.Queries.GetPublicContentByType;

public sealed class GetPublicContentByTypeQueryValidator : AbstractValidator<GetPublicContentByTypeQuery>
{
    public GetPublicContentByTypeQueryValidator()
    {
        RuleFor(x => x.Surface).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
