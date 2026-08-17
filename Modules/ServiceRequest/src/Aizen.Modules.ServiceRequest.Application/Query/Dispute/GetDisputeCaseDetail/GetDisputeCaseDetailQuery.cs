using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Modules.ServiceRequest.Application.Query.Dispute;

[DocumentationInfo("Dispute case detail query", "BE-S13a — composes the consolidated dispute case file for admin adjudication.")]
public sealed class GetDisputeCaseDetailQuery : AizenQuery<GetDisputeCaseDetailResponse>
{
    public long DisputeId { get; }
    public GetDisputeCaseDetailQuery(long disputeId) => DisputeId = disputeId;
}
