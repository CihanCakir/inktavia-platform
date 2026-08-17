using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Catalog;

public sealed class DeleteOfferCatalogItemBffCommand : AizenCommand<BffSuccessResult>
{
    public long Id { get; init; }
}
