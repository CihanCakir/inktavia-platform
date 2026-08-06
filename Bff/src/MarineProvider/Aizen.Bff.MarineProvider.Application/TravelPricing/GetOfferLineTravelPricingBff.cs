using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel;

namespace Aizen.Bff.MarineProvider.Application.TravelPricing;

/// <summary>S4 — the structured travel-pricing detail currently on one Travel offer line (null when none is set).</summary>
public sealed class GetOfferLineTravelPricingBffQuery : AizenQuery<TravelPricingDetailDto>
{
    public long OfferId { get; init; }
    public long ItemId  { get; init; }
}

public sealed class GetOfferLineTravelPricingBffQueryHandler
    : AizenQueryHandler<GetOfferLineTravelPricingBffQuery, TravelPricingDetailDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetOfferLineTravelPricingBffQueryHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest)
    {
        _resolver = resolver; _identityHolder = identityHolder; _serviceRequest = serviceRequest;
    }

    public override async Task<TravelPricingDetailDto?> Handle(
        GetOfferLineTravelPricingBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var result = await _serviceRequest.GetOfferLineTravelPricing(request.OfferId, request.ItemId);
        return result.Body;
    }
}
