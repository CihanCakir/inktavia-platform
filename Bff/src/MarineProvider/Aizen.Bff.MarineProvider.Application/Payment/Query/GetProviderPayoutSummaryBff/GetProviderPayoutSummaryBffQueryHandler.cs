using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class GetProviderPayoutSummaryBffQueryHandler
    : AizenQueryHandler<GetProviderPayoutSummaryBffQuery, ProviderPayoutSummaryDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly IPaymentRemoteCall _c;

    public GetProviderPayoutSummaryBffQueryHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IPaymentRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<ProviderPayoutSummaryDto?> Handle(GetProviderPayoutSummaryBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.GetPayoutSummary()).Body;
    }
}
