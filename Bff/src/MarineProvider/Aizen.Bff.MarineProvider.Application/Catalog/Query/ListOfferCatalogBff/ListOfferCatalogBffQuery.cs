using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Catalog;

public sealed class ListOfferCatalogBffQuery : AizenQuery<List<ProviderCatalogItemDto>>
{
}
