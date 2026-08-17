using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// Resolves the provider profile first (this populates the identity holder so the outgoing auth handler attaches the
/// trusted-BFF assertion), then passes through to the ServiceRequest module, which scopes disputes to the asserted
/// provider. Envelope-correct: returns the module response as-is (cost-free).
/// </summary>
public sealed class GetProviderDisputesBffQueryHandler
    : AizenQueryHandler<GetProviderDisputesBffQuery, GetProviderDisputesResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetProviderDisputesBffQueryHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder holder, IServiceRequestRemoteCall serviceRequest)
    {
        _resolver = resolver;
        _holder = holder;
        _serviceRequest = serviceRequest;
    }

    public override async Task<GetProviderDisputesResponse?> Handle(GetProviderDisputesBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_holder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        return (await _serviceRequest.GetProviderDisputes(q.PageIndex, q.PageSize, q.Status)).Body;
    }
}
