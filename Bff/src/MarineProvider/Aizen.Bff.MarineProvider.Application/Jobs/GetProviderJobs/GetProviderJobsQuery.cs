using Aizen.Bff.MarineProvider.Application.Contracts.Jobs;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Jobs.GetProviderJobs;

/// <summary>
/// Lists the caller provider's own assigned jobs. Provider identity is resolved from the verified Keycloak token
/// (never from parameters); only paging is accepted from the client.
/// </summary>
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
