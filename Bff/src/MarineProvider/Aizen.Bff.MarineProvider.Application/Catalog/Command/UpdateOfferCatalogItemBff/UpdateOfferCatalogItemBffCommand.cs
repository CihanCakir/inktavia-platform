using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Bff.MarineProvider.Application.Catalog;

public sealed class UpdateOfferCatalogItemBffCommand : AizenCommand<ProviderCatalogItemDto>
{
    public long Id { get; init; }
    public CatalogItemRequest Body { get; init; } = default!;
}
