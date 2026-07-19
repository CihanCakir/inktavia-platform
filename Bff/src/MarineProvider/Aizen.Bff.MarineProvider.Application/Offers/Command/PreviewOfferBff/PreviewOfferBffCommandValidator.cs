using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class PreviewOfferBffCommandValidator : AizenValidator<PreviewOfferBffCommand>
{
    public PreviewOfferBffCommandValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
        RuleFor(x => x.Body).NotNull();
    }
}
