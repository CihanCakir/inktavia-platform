using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

public sealed class GetProviderJobsWorkloadQuery : AizenQuery<GetProviderJobsWorkloadResponse>
{
    public int Weeks { get; }
    public GetProviderJobsWorkloadQuery(int weeks = 6) => Weeks = weeks;
}
