using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class DeleteOfferTemplateBffCommandValidator : AizenValidator<DeleteOfferTemplateBffCommand>
{
    public DeleteOfferTemplateBffCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
