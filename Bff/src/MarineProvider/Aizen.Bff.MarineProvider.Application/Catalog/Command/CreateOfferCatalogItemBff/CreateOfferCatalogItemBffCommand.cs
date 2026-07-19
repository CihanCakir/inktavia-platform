using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Bff.MarineProvider.Application.Catalog;

public sealed class CreateOfferCatalogItemBffCommand : AizenCommand<ProviderCatalogItemDto>
{
    public CatalogItemRequest Body { get; init; } = default!;
}
