using Aizen.Core.CQRS.Handler; using Aizen.Core.CQRS.Message; using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients; using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto; using Aizen.Modules.CargoDry.Abstraction.Enum;
namespace Aizen.Bff.MarineProvider.Application.CargoDry;
public sealed class GetCargoDryInventoryMovementsBffQueryHandler : AizenQueryHandler<GetCargoDryInventoryMovementsBffQuery, CargoDryInventoryMovementPagedResultDto>
{
    private readonly IProviderProfileResolver _resolver; private readonly IProviderIdentityHolder _h; private readonly ICargoDryRemoteCall _c;
    public GetCargoDryInventoryMovementsBffQueryHandler(IProviderProfileResolver r, IProviderIdentityHolder h, ICargoDryRemoteCall c) { _resolver = r; _h = h; _c = c; }
    public override async Task<CargoDryInventoryMovementPagedResultDto?> Handle(GetCargoDryInventoryMovementsBffQuery q, CancellationToken ct)
    { await _resolver.ResolveAsync(ct); if (_h.ProfileId is null or 0) throw new AizenBusinessException("Provider identity could not be resolved."); return (await _c.GetInventoryMovements(q.ProductCode, q.BatchCode, q.MovementType, q.DateFrom, q.DateTo, q.Page, q.PageSize)).Body; }
}
