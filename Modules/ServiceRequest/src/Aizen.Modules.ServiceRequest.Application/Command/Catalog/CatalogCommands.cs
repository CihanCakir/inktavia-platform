using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Catalog;

public sealed class CreateCatalogItemCommand : AizenCommand<ProviderCatalogItemDto>
{
    public CatalogItemRequest Request { get; }
    public CreateCatalogItemCommand(CatalogItemRequest request) => Request = request;
}

public sealed class UpdateCatalogItemCommand : AizenCommand<ProviderCatalogItemDto>
{
    public long ItemId { get; }
    public CatalogItemRequest Request { get; }
    public UpdateCatalogItemCommand(long itemId, CatalogItemRequest request) { ItemId = itemId; Request = request; }
}

public sealed class DeleteCatalogItemCommand : AizenCommand<bool>
{
    public long ItemId { get; }
    public DeleteCatalogItemCommand(long itemId) => ItemId = itemId;
}

public sealed class ListCatalogItemsQuery : AizenQuery<List<ProviderCatalogItemDto>> { }
