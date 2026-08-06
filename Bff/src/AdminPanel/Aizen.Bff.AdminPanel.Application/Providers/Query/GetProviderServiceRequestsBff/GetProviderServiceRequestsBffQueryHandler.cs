using Aizen.Bff.AdminPanel.Application.AdminProviders.Dto;
using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminProviders.Query;

[DocumentationInfo("Get provider service requests BFF query handler",
    "Fetches service requests assigned to a specific provider by passing providerProfileId to the " +
    "ServiceRequest admin list endpoint. Read-only enrichment — no SR assignment logic is changed.")]
public sealed class GetProviderServiceRequestsBffQueryHandler
    : AizenQueryHandler<GetProviderServiceRequestsBffQuery, ProviderServiceRequestsBffResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetProviderServiceRequestsBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<ProviderServiceRequestsBffResponse?> Handle(
        GetProviderServiceRequestsBffQuery request, CancellationToken cancellationToken)
    {
        var response = new ProviderServiceRequestsBffResponse();

        try
        {
            var result = await _serviceRequest.GetAdminServiceRequestList(
                status:            null,
                vesselId:          null,
                ownerUserId:       null,
                providerProfileId: request.ProviderProfileId,
                pageIndex:         request.PageIndex,
                pageSize:          request.PageSize);

            if (result?.Body?.Items != null)
            {
                var items = result.Body.Items.Select(s => new ServiceRequestListItemBffDto
                {
                    Id                  = s.Id,
                    RequestCode         = s.RequestCode,
                    VesselId            = s.VesselId,
                    ServiceType         = s.ServiceTypeCode ?? s.ServiceCategoryCode,
                    ServiceCategoryCode = s.ServiceCategoryCode,
                    Title               = s.Title,
                    Status              = s.Status.ToString().ToLowerInvariant(),
                    Priority            = s.Priority.ToString().ToLowerInvariant(),
                    Location            = s.LocationMarinaName,
                    Notes               = s.OwnerNotes ?? s.Title,
                    OwnerUserId         = s.OwnerUserId,
                    ProviderProfileId   = s.ProviderProfileId,
                    OfferCount          = s.OfferCount,
                    HasActiveAssignment = s.HasActiveAssignment,
                    RequestedDate       = s.RequestedStartDate,
                    LastActivityAt      = s.LastActivityAt,
                    CreatedAt           = s.CreatedAt,
                }).ToList();

                var total = result.Body.TotalCount;
                var pages = request.PageSize > 0
                    ? (int)Math.Ceiling((double)total / request.PageSize)
                    : 0;

                response.ServiceRequests = new ServiceRequestPageBffDto
                {
                    From        = request.PageIndex * request.PageSize,
                    Index       = request.PageIndex,
                    Size        = request.PageSize,
                    Count       = total,
                    Pages       = pages,
                    HasPrevious = request.PageIndex > 0,
                    HasNext     = request.PageIndex < pages - 1,
                    Items       = items,
                };
            }
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
