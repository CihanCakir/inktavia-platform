using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryCatalogProducts;

/// <summary>
/// Owner-safe active CargoDry product catalog (CargoDry supply flow). Returns only owner-visible fields + media file
/// ids — never commercial pricing. Consumed by the mobile BFF at GET /api/v1/cargodry/catalog/products.
/// </summary>
public sealed class GetCargoDryCatalogProductsQuery : AizenQuery<List<CargoDryProductCatalogDto>>
{
}
