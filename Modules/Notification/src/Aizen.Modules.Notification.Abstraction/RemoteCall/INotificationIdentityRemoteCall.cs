using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Modules.Notification.Abstraction.RemoteCall;

/// <summary>
/// N-C — resolves region-eligible providers from the Identity I2 read-model (GetProvidersForArea). Internal
/// module-to-module read (Identity endpoint is anonymous behind the cluster NetworkPolicy — same pattern as
/// ServiceRequest→ReferenceData). Auto-registered by the Core remote-call scan; base URL from
/// RemoteCalls__INotificationIdentityRemoteCall__BaseUrl.
/// </summary>
public interface INotificationIdentityRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/identity/providers/for-area")]
    Task<AizenApiResponse<List<ProviderForAreaResult>>> GetProvidersForArea(
        [Refit.Query] string cityCode,
        [Refit.Query] string? categoryCode = null,
        [Refit.Query] int take = 500);
}

/// <summary>BFF/module-local mirror of Identity's ProviderForAreaDto (deserialized by JSON property name).</summary>
public sealed class ProviderForAreaResult
{
    public long ProfileId { get; set; }
    public long UserId    { get; set; }
}
