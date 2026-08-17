using Aizen.Core.CQRS.Handler; using Aizen.Core.CQRS.Message; using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients; using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto;
namespace Aizen.Bff.MarineProvider.Application.CargoDry;
public sealed class CancelCargoDryStockRequestBffCommandHandler : AizenCommandHandler<CancelCargoDryStockRequestBffCommand, CargoDryStockRequestDto>
{
    private readonly IProviderProfileResolver _resolver; private readonly IProviderIdentityHolder _h; private readonly ICargoDryRemoteCall _c;
    public CancelCargoDryStockRequestBffCommandHandler(IProviderProfileResolver r, IProviderIdentityHolder h, ICargoDryRemoteCall c) { _resolver = r; _h = h; _c = c; }
    public override async Task<CargoDryStockRequestDto?> Handle(CancelCargoDryStockRequestBffCommand cmd, CancellationToken ct)
    { await _resolver.ResolveAsync(ct); if (_h.ProfileId is null or 0) throw new AizenBusinessException("Provider identity could not be resolved."); return (await _c.CancelStockRequest(cmd.Id)).Body; }
}
