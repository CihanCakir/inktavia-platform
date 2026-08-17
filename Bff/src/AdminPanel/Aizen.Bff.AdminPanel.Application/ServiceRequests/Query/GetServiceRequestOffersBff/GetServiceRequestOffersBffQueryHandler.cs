using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin service request offers query handler", "Fetches provider offers and agreement timeline for a service request.")]
public sealed class GetServiceRequestOffersBffQueryHandler
    : AizenQueryHandler<GetServiceRequestOffersBffQuery, AdminServiceRequestOffersResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetServiceRequestOffersBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminServiceRequestOffersResponse?> Handle(
        GetServiceRequestOffersBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestOffersResponse();

        try
        {

            var result = await _serviceRequest.GetAdminServiceRequestOffers(
                request.ServiceRequestId);
            response.Offers = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
