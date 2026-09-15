using Aizen.Core.CQRS.Message;
using Aizen.Bff.MarineProvider.Application.CargoDry.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

public sealed class GetCargoDryCatalogBffQuery : AizenQuery<List<CargoDryProviderCatalogItemBffDto>> { }
