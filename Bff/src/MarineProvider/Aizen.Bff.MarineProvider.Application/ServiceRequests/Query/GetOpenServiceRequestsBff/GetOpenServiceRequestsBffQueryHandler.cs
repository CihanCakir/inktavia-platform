using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetOpenServiceRequestsBffQueryHandler
    : AizenQueryHandler<GetOpenServiceRequestsBffQuery, GetOpenServiceRequestsResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<GetOpenServiceRequestsBffQueryHandler> _logger;

    public GetOpenServiceRequestsBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<GetOpenServiceRequestsBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<GetOpenServiceRequestsResponse?> Handle(
        GetOpenServiceRequestsBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
        {
            _logger.LogWarning("Provider profile not resolved. Returning empty open SR list.");
            return new GetOpenServiceRequestsResponse { PageIndex = request.PageIndex, PageSize = request.PageSize };
        }

        var result = await _serviceRequest.GetOpenServiceRequests(
            request.PageIndex, request.PageSize,
            request.ServiceCategoryCode, request.LocationCityCode,
            request.LocationCountryCode, request.SearchTerm);

        return result.Body;
    }
}
