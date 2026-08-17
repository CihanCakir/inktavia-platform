using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

/// <summary>BE-S13a — passthrough for the consolidated dispute case file.</summary>
public sealed class GetDisputeCaseBffQuery : AizenQuery<GetDisputeCaseDetailResponse>
{
    public long ServiceRequestId { get; }
    public long DisputeId { get; }
    public GetDisputeCaseBffQuery(long serviceRequestId, long disputeId)
    {
        ServiceRequestId = serviceRequestId;
        DisputeId = disputeId;
    }
}
