using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto;
namespace Aizen.Bff.MarineProvider.Application.CargoDry;
public sealed class GetCargoDryOverviewBffQueryHandler : AizenQueryHandler<GetCargoDryOverviewBffQuery, CargoDryOperationalOverviewDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly ICargoDryRemoteCall _cargoDry;

    public GetCargoDryOverviewBffQueryHandler(IProviderProfileResolver resolver, IProviderIdentityHolder identityHolder, ICargoDryRemoteCall cargoDry)
    { _resolver = resolver; _identityHolder = identityHolder; _cargoDry = cargoDry; }

    public override async Task<CargoDryOperationalOverviewDto?> Handle(GetCargoDryOverviewBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0) throw new AizenBusinessException("Provider identity could not be resolved.");
        var result = await _cargoDry.GetOverview();
        return result.Body;
    }
}
