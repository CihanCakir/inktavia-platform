using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class GetJobsWorkloadBffQuery : AizenQuery<GetProviderJobsWorkloadResponse>
{
    public int Weeks { get; init; } = 6;
}
