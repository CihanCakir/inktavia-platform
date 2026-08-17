using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Users.Query;

[DocumentationInfo("Get admin user service requests BFF query handler",
    "Fetches paged service requests owned by a specific user. Calls the ServiceRequest module admin list " +
    "endpoint with ownerUserId filter. Used by the User Detail page service-requests tab.")]
public sealed class GetUserServiceRequestsBffQueryHandler
    : AizenQueryHandler<GetUserServiceRequestsBffQuery, AdminServiceRequestListResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetUserServiceRequestsBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<AdminServiceRequestListResponse?> Handle(
        GetUserServiceRequestsBffQuery request, CancellationToken ct)
    {
        var response = new AdminServiceRequestListResponse();

        try
        {
            // pageIndex is 0-based; BFF query uses 1-based Page → convert
            var pageIndex = request.Page > 0 ? request.Page - 1 : 0;

            var result = await _serviceRequest.GetAdminServiceRequestList(
                status:      request.Status,
                vesselId:    null,
                ownerUserId: request.ProfileId,
                pageIndex:   pageIndex,
                pageSize:    request.PageSize);

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
                var pages = request.PageSize > 0 ? (int)Math.Ceiling((double)total / request.PageSize) : 0;

                response.ServiceRequests = new ServiceRequestPageBffDto
                {
                    From        = pageIndex * request.PageSize,
                    Index       = pageIndex,
                    Size        = request.PageSize,
                    Count       = total,
                    Pages       = pages,
                    HasPrevious = pageIndex > 0,
                    HasNext     = pageIndex < pages - 1,
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
