using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class UpdateOfferBffCommandValidator : AizenValidator<UpdateOfferBffCommand>
{
    public UpdateOfferBffCommandValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
        RuleFor(x => x.OfferId).GreaterThan(0);
        RuleFor(x => x.Body).NotNull();
    }
}
