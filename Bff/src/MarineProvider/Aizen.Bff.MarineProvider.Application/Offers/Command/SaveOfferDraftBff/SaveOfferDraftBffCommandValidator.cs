using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class SaveOfferDraftBffCommandValidator : AizenValidator<SaveOfferDraftBffCommand>
{
    public SaveOfferDraftBffCommandValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
        RuleFor(x => x.Body).NotNull();
    }
}
