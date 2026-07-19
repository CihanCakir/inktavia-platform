using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class SubmitOfferBffCommandValidator : AizenValidator<SubmitOfferBffCommand>
{
    public SubmitOfferBffCommandValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
        RuleFor(x => x.OfferId).GreaterThan(0);
        RuleFor(x => x.Body).NotNull();
    }
}
