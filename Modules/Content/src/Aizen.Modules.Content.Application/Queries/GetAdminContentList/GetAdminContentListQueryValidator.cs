using FluentValidation;

namespace Aizen.Modules.Content.Application.Queries.GetAdminContentList;

public sealed class GetAdminContentListQueryValidator : AbstractValidator<GetAdminContentListQuery>
{
    public GetAdminContentListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
