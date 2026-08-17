using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class CreateOfferTemplateBffCommandValidator : AizenValidator<CreateOfferTemplateBffCommand>
{
    public CreateOfferTemplateBffCommandValidator()
    {
        RuleFor(x => x.Body).NotNull();
    }
}
