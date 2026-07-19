using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class GetMyOffersBffQueryValidator : AizenValidator<GetMyOffersBffQuery>
{
    public GetMyOffersBffQueryValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
