using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Catalog;

public sealed class DeleteOfferCatalogItemBffCommandValidator : AizenValidator<DeleteOfferCatalogItemBffCommand>
{
    public DeleteOfferCatalogItemBffCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
