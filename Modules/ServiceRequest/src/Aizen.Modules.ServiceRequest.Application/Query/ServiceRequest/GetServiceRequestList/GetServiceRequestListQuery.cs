using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;

[DocumentationInfo("Get service request list query", "Returns paginated list of service requests for the current user.")]
public sealed class GetServiceRequestListQuery : AizenQuery<GetServiceRequestListResponse>
{
    public long OwnerUserId { get; }
    public ServiceRequestListFilterRequest Filter { get; }
    public GetServiceRequestListQuery(long ownerUserId, ServiceRequestListFilterRequest filter)
    {
        OwnerUserId = ownerUserId; Filter = filter;
    }
}
