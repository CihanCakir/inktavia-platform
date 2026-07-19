using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class CreateOfferBffCommandValidator : AizenValidator<CreateOfferBffCommand>
{
    public CreateOfferBffCommandValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
        RuleFor(x => x.Body).NotNull();
    }
}
