using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetOwnerDisputes;

/// <summary>
/// BE-MO5 — returns the calling owner's disputes (on the SRs they own) + a global open/actionable count. Owner
/// identity is taken from the trusted context (BFF assertion), never from parameters. Optionally narrowed to one
/// status. Mirrors <c>GetProviderDisputesQuery</c>.
/// </summary>
[DocumentationInfo("Get owner disputes query", "Lists the caller owner's disputes + open count. Identity from the trusted context, never parameters.")]
public sealed class GetOwnerDisputesQuery : AizenQuery<GetOwnerDisputesResponse>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public ServiceRequestDisputeStatus? StatusFilter { get; init; }

    public GetOwnerDisputesQuery(int pageIndex = 0, int pageSize = 20)
    {
        PageIndex = pageIndex < 0 ? 0 : pageIndex;
        PageSize = pageSize is <= 0 or > 100 ? 20 : pageSize;
    }
}
