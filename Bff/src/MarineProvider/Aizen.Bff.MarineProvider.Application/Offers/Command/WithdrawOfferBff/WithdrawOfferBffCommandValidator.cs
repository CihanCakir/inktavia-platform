using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class WithdrawOfferBffCommandValidator : AizenValidator<WithdrawOfferBffCommand>
{
    public WithdrawOfferBffCommandValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
        RuleFor(x => x.OfferId).GreaterThan(0);
    }
}
