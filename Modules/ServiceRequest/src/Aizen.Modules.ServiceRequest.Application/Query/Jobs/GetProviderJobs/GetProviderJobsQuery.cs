using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

[DocumentationInfo("Get provider jobs query", "Lists the caller provider's own assignments (jobs). Provider identity is taken from the trusted context, never from parameters.")]
public sealed class GetProviderJobsQuery : AizenQuery<GetProviderJobsResponse>
{
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetProviderJobsQuery(int pageIndex = 0, int pageSize = 50)
    {
        PageIndex = pageIndex < 0 ? 0 : pageIndex;
        PageSize = pageSize is <= 0 or > 200 ? 50 : pageSize;
    }
}
