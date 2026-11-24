using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Text;
using Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Catalogue.Query.GetWebServiceCatalogue;

public sealed class GetWebServiceCatalogueQueryHandler
    : AizenQueryHandler<GetWebServiceCatalogueQuery, List<WebServiceSummaryDto>>
{
    /// <summary>The ReferenceData lookup group that holds the marketplace service taxonomy.</summary>
    private const string ServiceCategoryGroupCode = "SERVICE_PROVIDER_CATEGORY";

    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetWebServiceCatalogueQueryHandler> _logger;

    public GetWebServiceCatalogueQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetWebServiceCatalogueQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<List<WebServiceSummaryDto>?> Handle(
        GetWebServiceCatalogueQuery request, CancellationToken cancellationToken)
    {
        var locale = WebLocale.Normalize(request.Lang);

        try
        {
            // Active service categories only (onlyActive=true default); the module returns clean lookup items which
            // the mapper strips to web tiles.
            var resp = await _reference.GetLookupItems(ServiceCategoryGroupCode);
            var items = resp?.Body ?? new();
            return items
                .OrderBy(i => i.SortOrder)
                // Ordered by the name the READER sees, not the English one, and collated in the REQUESTED locale —
                // Turkish orders ç, ğ, ı, ö, ş, ü as their own letters. The culture is derived from the URL rather
                // than read from the server's ambient culture, so the same request always sorts the same way (which
                // also keeps the response body, and therefore its ETag, stable across hosts).
                .ThenBy(i => WebServiceMapper.ResolveName(i, locale), WebLocale.NameComparer(locale))
                .Select(i => WebServiceMapper.ToSummary(i, locale))
                .ToList();
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Web service catalogue failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("The service catalogue is currently unavailable.");
        }
    }
}
