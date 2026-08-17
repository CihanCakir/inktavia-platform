using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class GetMyOffersBffQueryHandler
    : AizenQueryHandler<GetMyOffersBffQuery, GetMyOffersResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<GetMyOffersBffQueryHandler> _logger;

    public GetMyOffersBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<GetMyOffersBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<GetMyOffersResponse?> Handle(GetMyOffersBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
        {
            _logger.LogWarning("Provider profile not resolved. Returning empty offers list.");
            return new GetMyOffersResponse { PageIndex = request.PageIndex, PageSize = request.PageSize };
        }

        var result = await _serviceRequest.GetMyOffers(
            request.PageIndex, request.PageSize, request.Status);

        return result.Body;
    }
}
