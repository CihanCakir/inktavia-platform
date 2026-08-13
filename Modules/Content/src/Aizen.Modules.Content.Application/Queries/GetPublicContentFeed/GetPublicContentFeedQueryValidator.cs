using FluentValidation;

namespace Aizen.Modules.Content.Application.Queries.GetPublicContentFeed;

public sealed class GetPublicContentFeedQueryValidator : AbstractValidator<GetPublicContentFeedQuery>
{
    public GetPublicContentFeedQueryValidator()
    {
        RuleFor(x => x.Surface).IsInEnum();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
