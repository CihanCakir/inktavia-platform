using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebLookupItems;

public sealed class GetWebLookupItemsQueryHandler
    : AizenQueryHandler<GetWebLookupItemsQuery, List<LookupItemDto>>
{
    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetWebLookupItemsQueryHandler> _logger;

    public GetWebLookupItemsQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetWebLookupItemsQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<List<LookupItemDto>?> Handle(
        GetWebLookupItemsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var resp = await _reference.GetLookupItems(request.GroupCode);
            // Passthrough: reference DTO is clean, public lookup data; ordered for stable presentation.
            var items = resp?.Body ?? new List<LookupItemDto>();
            return items.OrderBy(i => i.SortOrder).ThenBy(i => i.Name).ToList();
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Get web lookup items for {GroupCode} failed (status {Status}).",
                request.GroupCode, ex.StatusCode);
            throw new AizenBusinessException("Lookup reference data is currently unavailable.");
        }
    }
}
