using FluentValidation;

namespace Aizen.Modules.Content.Application.Queries.GetContentComments;

public sealed class GetContentCommentsQueryValidator : AbstractValidator<GetContentCommentsQuery>
{
    public GetContentCommentsQueryValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
