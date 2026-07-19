using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto;
namespace Aizen.Bff.MarineProvider.Application.CargoDry;

public sealed class GetCargoDryAlertsBffQueryHandler : AizenQueryHandler<GetCargoDryAlertsBffQuery, CargoDryOperationalAlertsResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly ICargoDryRemoteCall _cargoDry;
    public GetCargoDryAlertsBffQueryHandler(IProviderProfileResolver r, IProviderIdentityHolder h, ICargoDryRemoteCall c)
    { _resolver = r; _identityHolder = h; _cargoDry = c; }
    public override async Task<CargoDryOperationalAlertsResponse?> Handle(GetCargoDryAlertsBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0) throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _cargoDry.GetAlerts(1, q.Take)).Body;
    }
}
