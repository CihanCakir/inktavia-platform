using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

public sealed class GetCargoDrySettlementsBffQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementsBffQuery, CargoDryProviderSettlementPagedResultDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly ICargoDryRemoteCall _c;

    public GetCargoDrySettlementsBffQueryHandler(IProviderProfileResolver r, IProviderIdentityHolder h, ICargoDryRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<CargoDryProviderSettlementPagedResultDto?> Handle(GetCargoDrySettlementsBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.GetSettlements(q.Status, q.Page, q.PageSize, q.From, q.To)).Body;
    }
}
