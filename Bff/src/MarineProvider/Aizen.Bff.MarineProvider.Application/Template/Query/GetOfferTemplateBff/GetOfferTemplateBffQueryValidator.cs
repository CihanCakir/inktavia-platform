using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class GetOfferTemplateBffQueryValidator : AizenValidator<GetOfferTemplateBffQuery>
{
    public GetOfferTemplateBffQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
