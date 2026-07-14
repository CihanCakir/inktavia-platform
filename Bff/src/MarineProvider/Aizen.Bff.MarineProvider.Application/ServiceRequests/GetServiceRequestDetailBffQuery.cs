using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetServiceRequestDetailBffQuery : AizenQuery<GetServiceRequestDetailResponse>
{
    public long ServiceRequestId { get; init; }
}
