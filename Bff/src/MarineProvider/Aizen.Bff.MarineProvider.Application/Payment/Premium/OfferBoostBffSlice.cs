using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Request;

namespace Aizen.Bff.MarineProvider.Application.Payment.Premium;

// ─── BE-P11 PurchaseOfferBoost (command) ─────────────────────────────────────
public sealed class PurchaseOfferBoostBffCommand : AizenCommand<PurchaseOfferBoostResult>
{
    public long   OfferId      { get; init; }
    public string CurrencyCode { get; init; } = "TRY";
}

public sealed class PurchaseOfferBoostBffCommandHandler
    : AizenCommandHandler<PurchaseOfferBoostBffCommand, PurchaseOfferBoostResult>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder  _h;
    private readonly IPaymentRemoteCall        _c;

    public PurchaseOfferBoostBffCommandHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IPaymentRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<PurchaseOfferBoostResult?> Handle(PurchaseOfferBoostBffCommand cmd, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.PurchaseOfferBoost(new PurchaseOfferBoostRequest
        {
            OfferId      = cmd.OfferId,
            CurrencyCode = cmd.CurrencyCode,
        })).Body;
    }
}

// ─── BE-P11 GetOfferBoostStatus (query) ──────────────────────────────────────
public sealed class GetOfferBoostStatusBffQuery : AizenQuery<OfferBoostStatusDto>
{
    public long OfferId { get; init; }
}

public sealed class GetOfferBoostStatusBffQueryHandler
    : AizenQueryHandler<GetOfferBoostStatusBffQuery, OfferBoostStatusDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder  _h;
    private readonly IPaymentRemoteCall        _c;

    public GetOfferBoostStatusBffQueryHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IPaymentRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<OfferBoostStatusDto?> Handle(GetOfferBoostStatusBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.GetOfferBoostStatus(q.OfferId)).Body;
    }
}
