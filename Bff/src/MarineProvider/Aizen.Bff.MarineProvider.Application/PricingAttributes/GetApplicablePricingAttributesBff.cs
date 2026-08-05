using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.PricingAttributes;

/// <summary>S2 — the pricing attributes applicable to an SR's category, with resolved Lookup options (provider offer picker).</summary>
public sealed class GetApplicablePricingAttributesBffQuery : AizenQuery<List<ApplicablePricingAttributeDto>>
{
    public long ServiceRequestId { get; init; }
}

public sealed class GetApplicablePricingAttributesBffQueryHandler
    : AizenQueryHandler<GetApplicablePricingAttributesBffQuery, List<ApplicablePricingAttributeDto>>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<GetApplicablePricingAttributesBffQueryHandler> _logger;

    public GetApplicablePricingAttributesBffQueryHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest, ILogger<GetApplicablePricingAttributesBffQueryHandler> logger)
    {
        _resolver = resolver; _identityHolder = identityHolder; _serviceRequest = serviceRequest; _logger = logger;
    }

    public override async Task<List<ApplicablePricingAttributeDto>?> Handle(
        GetApplicablePricingAttributesBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var result = await _serviceRequest.GetApplicablePricingAttributes(request.ServiceRequestId);
        return result.Body ?? new List<ApplicablePricingAttributeDto>();
    }
}
