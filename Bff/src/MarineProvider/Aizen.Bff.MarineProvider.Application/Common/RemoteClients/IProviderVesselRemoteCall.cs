using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

public interface IProviderVesselRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/vessels/summary")]
    Task<AizenApiResponse<GetVesselSummariesResponse>> GetSummaries([Refit.Query] string ids);
}
