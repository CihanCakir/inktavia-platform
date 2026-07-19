using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// Provider-scoped messages: returns conversation history + channelOpen state.
/// Access-checks the provider's relationship to the SR via the detail endpoint (same assertion flow).
/// </summary>
public sealed class GetProviderMessagesQueryHandler
    : AizenQueryHandler<GetProviderMessagesQuery, ProviderMessagesResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<GetProviderMessagesQueryHandler> _logger;

    public GetProviderMessagesQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<GetProviderMessagesQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<ProviderMessagesResponse?> Handle(GetProviderMessagesQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _serviceRequest.GetMessages(request.ServiceRequestId, request.Skip, request.Take);
            var messages = result.Body?.Messages ?? new();

            var channelOpen = messages.Any(m => m.SenderType == ServiceRequestMessageSenderType.Owner);

            return new ProviderMessagesResponse
            {
                Items = messages,
                ChannelOpen = channelOpen,
                TotalCount = result.Body?.TotalCount ?? messages.Count
            };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "GetMessages failed for SR {ServiceRequestId}, provider {ProfileId}",
                request.ServiceRequestId, _identityHolder.ProfileId);
            throw new AizenBusinessException("Service request not found.");
        }
    }
}
