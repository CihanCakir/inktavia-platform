using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

/// <summary>Provider confirms receipt of a Shipped stock request (portal). Shipped → Received.</summary>
public sealed class ReceiveCargoDryStockRequestBffCommand : AizenCommand<CargoDryStockRequestDto> { public long Id { get; init; } }

public sealed class ReceiveCargoDryStockRequestBffCommandHandler
    : AizenCommandHandler<ReceiveCargoDryStockRequestBffCommand, CargoDryStockRequestDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder  _h;
    private readonly ICargoDryRemoteCall      _c;

    public ReceiveCargoDryStockRequestBffCommandHandler(
        IProviderProfileResolver r, IProviderIdentityHolder h, ICargoDryRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<CargoDryStockRequestDto?> Handle(ReceiveCargoDryStockRequestBffCommand cmd, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0) throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.ReceiveStockRequest(cmd.Id)).Body;
    }
}
