using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
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
        try
        {
            // Active service categories only (onlyActive=true default); the module returns clean lookup items which
            // the mapper strips to web tiles.
            var resp = await _reference.GetLookupItems(ServiceCategoryGroupCode);
            var items = resp?.Body ?? new();
            return items
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Name)
                .Select(WebServiceMapper.ToSummary)
                .ToList();
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Web service catalogue failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("The service catalogue is currently unavailable.");
        }
    }
}
