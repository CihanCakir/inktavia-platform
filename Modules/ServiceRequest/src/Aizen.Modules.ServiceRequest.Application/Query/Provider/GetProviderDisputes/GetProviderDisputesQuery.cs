using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDisputes;

/// <summary>
/// Returns the calling provider's disputes (on the SRs they won) + a global open/actionable count. Provider identity
/// is taken from the trusted context (BFF assertion), never from parameters. Optionally narrowed to one status.
/// </summary>
[DocumentationInfo("Get provider disputes query", "Lists the caller provider's disputes + open count. Identity from the trusted context, never parameters.")]
public sealed class GetProviderDisputesQuery : AizenQuery<GetProviderDisputesResponse>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public ServiceRequestDisputeStatus? StatusFilter { get; init; }

    public GetProviderDisputesQuery(int pageIndex = 0, int pageSize = 20)
    {
        PageIndex = pageIndex < 0 ? 0 : pageIndex;
        PageSize = pageSize is <= 0 or > 100 ? 20 : pageSize;
    }
}
