using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// Provider's own disputes (+ global open/actionable count). Provider identity is resolved server-side and asserted
/// to the ServiceRequest module — the client never passes a provider id.
/// </summary>
public sealed class GetProviderDisputesBffQuery : AizenQuery<GetProviderDisputesResponse>
{
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public ServiceRequestDisputeStatus? Status { get; init; }
}
