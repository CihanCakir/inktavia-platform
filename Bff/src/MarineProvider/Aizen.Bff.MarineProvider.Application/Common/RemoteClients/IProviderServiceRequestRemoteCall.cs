using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

/// <summary>
/// BFF → ServiceRequest module calls (provider-scoped reads). Auth (Keycloak service token) + the trusted-BFF
/// identity assertion headers are injected automatically by <c>MarineProviderBffAuthDelegatingHandler</c>, so the
/// module scopes results by the asserted provider profile id.
/// </summary>
public interface IProviderServiceRequestRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/service-requests/provider/jobs")]
    Task<AizenApiResponse<GetProviderJobsResponse>> GetProviderJobs(
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 50);
}
