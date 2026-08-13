using FluentValidation;

namespace Aizen.Modules.Content.Application.Queries.GetAdminContentComments;

public sealed class GetAdminContentCommentsQueryValidator : AbstractValidator<GetAdminContentCommentsQuery>
{
    public GetAdminContentCommentsQueryValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
