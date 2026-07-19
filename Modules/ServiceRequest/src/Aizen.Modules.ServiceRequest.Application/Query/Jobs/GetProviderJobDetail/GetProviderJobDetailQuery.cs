using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

public sealed class GetProviderJobDetailQuery : AizenQuery<GetProviderJobDetailResponse>
{
    public long AssignmentId { get; }
    public GetProviderJobDetailQuery(long assignmentId) => AssignmentId = assignmentId;
}
