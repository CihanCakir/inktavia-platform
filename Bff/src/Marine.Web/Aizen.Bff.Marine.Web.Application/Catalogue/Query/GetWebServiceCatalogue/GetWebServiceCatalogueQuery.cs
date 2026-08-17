using Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Catalogue.Query.GetWebServiceCatalogue;

/// <summary>
/// GET /api/v1/web/services — the public service-category catalogue grid (W4). Backed by the ReferenceData
/// <c>SERVICE_PROVIDER_CATEGORY</c> lookup group (the only reachable source today); reshaped to the web tile DTO.
/// </summary>
public sealed class GetWebServiceCatalogueQuery : AizenQuery<List<WebServiceSummaryDto>>
{
}
