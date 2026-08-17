using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Catalog;

public sealed class UpdateOfferCatalogItemBffCommandValidator : AizenValidator<UpdateOfferCatalogItemBffCommand>
{
    public UpdateOfferCatalogItemBffCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Body).NotNull();
    }
}
