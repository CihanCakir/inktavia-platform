using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get admin service request list query handler", "Fetches the paged admin service request list with optional status and vessel filters.")]
public sealed class GetAdminServiceRequestListQueryHandler
    : AizenQueryHandler<GetAdminServiceRequestListQuery, AdminServiceRequestListResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminServiceRequestListQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminServiceRequestListResponse?> Handle(
        GetAdminServiceRequestListQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestListResponse();

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            var authHeader = $"Bearer {serviceToken}";

            var result = await _serviceRequest.GetAdminServiceRequestList(
                authHeader, request.UserToken, request.Status, request.VesselId, request.PageIndex, request.PageSize);

            if (result?.Body?.Items != null)
            {
                var items = result.Body.Items.Select(s => new ServiceRequestListItemBffDto
                {
                    Id = s.Id,
                    RequestCode = s.RequestCode,
                    VesselId = s.VesselId,
                    ServiceType = s.ServiceTypeCode ?? s.ServiceCategoryCode,
                    ServiceCategoryCode = s.ServiceCategoryCode,
                    Title = s.Title,
                    Status = s.Status.ToString().ToLowerInvariant(),
                    Priority = s.Priority.ToString().ToLowerInvariant(),
                    Location = s.LocationMarinaName,
                    Notes = s.OwnerNotes ?? s.Title,
                    OwnerUserId = s.OwnerUserId,
                    ProviderProfileId = s.ProviderProfileId,
                    OfferCount = s.OfferCount,
                    HasActiveAssignment = s.HasActiveAssignment,
                    RequestedDate = s.RequestedStartDate,
                    LastActivityAt = s.LastActivityAt,
                    CreatedAt = s.CreatedAt
                }).ToList();

                var total = result.Body.TotalCount;
                var pages = request.PageSize > 0 ? (int)Math.Ceiling((double)total / request.PageSize) : 0;

                response.ServiceRequests = new ServiceRequestPageBffDto
                {
                    From = request.PageIndex * request.PageSize,
                    Index = request.PageIndex,
                    Size = request.PageSize,
                    Count = total,
                    Pages = pages,
                    HasPrevious = request.PageIndex > 0,
                    HasNext = request.PageIndex < pages - 1,
                    Items = items
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
