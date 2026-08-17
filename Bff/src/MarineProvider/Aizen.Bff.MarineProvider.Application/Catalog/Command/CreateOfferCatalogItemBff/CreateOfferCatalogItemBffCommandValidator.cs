using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Catalog;

public sealed class CreateOfferCatalogItemBffCommandValidator : AizenValidator<CreateOfferCatalogItemBffCommand>
{
    public CreateOfferCatalogItemBffCommandValidator()
    {
        RuleFor(x => x.Body).NotNull();
    }
}
