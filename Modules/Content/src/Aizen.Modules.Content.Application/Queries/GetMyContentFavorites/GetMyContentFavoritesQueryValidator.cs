using FluentValidation;

namespace Aizen.Modules.Content.Application.Queries.GetMyContentFavorites;

public sealed class GetMyContentFavoritesQueryValidator : AbstractValidator<GetMyContentFavoritesQuery>
{
    public GetMyContentFavoritesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
