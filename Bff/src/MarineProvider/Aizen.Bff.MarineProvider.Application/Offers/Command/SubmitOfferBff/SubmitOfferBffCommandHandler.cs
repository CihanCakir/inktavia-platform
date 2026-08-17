using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class SubmitOfferBffCommandHandler
    : AizenCommandHandler<SubmitOfferBffCommand, SubmitOfferResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<SubmitOfferBffCommandHandler> _logger;

    public SubmitOfferBffCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<SubmitOfferBffCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<SubmitOfferResponse?> Handle(SubmitOfferBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _serviceRequest.SubmitOffer(request.ServiceRequestId, request.OfferId, request.Body);
            return result.Body;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "SubmitOffer failed for SR {ServiceRequestId}, offer {OfferId}",
                request.ServiceRequestId, request.OfferId);
            throw new AizenBusinessException(ExtractMessage(ex.Content) ?? "Failed to submit offer.");
        }
    }

    private static string? ExtractMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("header", out var h) && h.TryGetProperty("errorMessage", out var m))
                return m.GetString();
        }
        catch { }
        return null;
    }
}
