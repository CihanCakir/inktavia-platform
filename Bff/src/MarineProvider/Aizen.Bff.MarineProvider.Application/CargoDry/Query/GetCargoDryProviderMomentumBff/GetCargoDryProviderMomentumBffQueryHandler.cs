using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

public sealed class GetCargoDryProviderMomentumBffQueryHandler
    : AizenQueryHandler<GetCargoDryProviderMomentumBffQuery, CargoDryProviderMomentumDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly ICargoDryRemoteCall _c;

    public GetCargoDryProviderMomentumBffQueryHandler(IProviderProfileResolver r, IProviderIdentityHolder h, ICargoDryRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<CargoDryProviderMomentumDto?> Handle(GetCargoDryProviderMomentumBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.GetMomentum()).Body;
    }
}
