using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.PricingAttributes;

/// <summary>S2 — the pricing attribute values currently set on one offer line.</summary>
public sealed class GetOfferLineAttributesBffQuery : AizenQuery<List<PricingAttributeValueDto>>
{
    public long OfferId { get; init; }
    public long ItemId  { get; init; }
}

public sealed class GetOfferLineAttributesBffQueryHandler
    : AizenQueryHandler<GetOfferLineAttributesBffQuery, List<PricingAttributeValueDto>>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetOfferLineAttributesBffQueryHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest)
    {
        _resolver = resolver; _identityHolder = identityHolder; _serviceRequest = serviceRequest;
    }

    public override async Task<List<PricingAttributeValueDto>?> Handle(
        GetOfferLineAttributesBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var result = await _serviceRequest.GetOfferLineAttributes(request.OfferId, request.ItemId);
        return result.Body ?? new List<PricingAttributeValueDto>();
    }
}
