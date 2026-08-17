using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class UpdateOfferTemplateBffCommandValidator : AizenValidator<UpdateOfferTemplateBffCommand>
{
    public UpdateOfferTemplateBffCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Body).NotNull();
    }
}
