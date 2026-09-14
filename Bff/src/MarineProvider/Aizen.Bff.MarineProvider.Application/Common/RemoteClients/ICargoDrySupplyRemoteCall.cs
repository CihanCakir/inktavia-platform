using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall.Requests;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall.Responses;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

/// <summary>
/// Provider BFF → CargoDry internal supply endpoints. Auth is injected by MarineProviderBffAuthDelegatingHandler
/// (the caller token is forwarded), so no Authorization param here — matching the other provider BFF remote calls.
/// </summary>
public interface ICargoDrySupplyRemoteCall : IAizenRemoteCall
{
    /// <summary>
    /// Batch discovery context: which of the given product codes the provider may accept (active agreement + active
    /// product) + which owners prefer this provider. One call per discovery page to compute canAccept + isPreferred.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/cargodry/internal/supply/provider-context")]
    Task<GetCargoDrySupplyProviderContextRemoteResponse> GetProviderContext(
        [AizenRemoteCallBody] GetCargoDrySupplyProviderContextRemoteRequest request);
}
