using Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Catalogue.Query.GetWebServiceCatalogue;

/// <summary>
/// GET /api/v1/web/services?lang — the public service-category catalogue grid (W4). Backed by the ReferenceData
/// <c>SERVICE_PROVIDER_CATEGORY</c> lookup group (the only reachable source today); reshaped to the web tile DTO.
/// </summary>
public sealed class GetWebServiceCatalogueQuery : AizenQuery<List<WebServiceSummaryDto>>
{
    /// <summary>
    /// Requested display locale (<c>tr</c> / <c>en</c>). Unrecognised or absent resolves to the default, so a caller
    /// that predates this parameter gets exactly the response it got before. Every tile returns the full slug map
    /// regardless, so one call serves a page and its locale switcher.
    /// </summary>
    public string? Lang { get; set; }
}
