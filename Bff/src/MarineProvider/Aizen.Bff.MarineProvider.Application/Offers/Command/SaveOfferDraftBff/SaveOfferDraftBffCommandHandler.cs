using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Offers;

/// <summary>
/// Aggregate draft save. Totals passed through from the module untouched — the BFF computes nothing.
/// </summary>
public sealed class SaveOfferDraftBffCommandHandler
    : AizenCommandHandler<SaveOfferDraftBffCommand, SaveOfferDraftResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<SaveOfferDraftBffCommandHandler> _logger;

    public SaveOfferDraftBffCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<SaveOfferDraftBffCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<SaveOfferDraftResponse?> Handle(SaveOfferDraftBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _serviceRequest.SaveOfferDraft(request.ServiceRequestId, request.Body);
            return result.Body;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "SaveOfferDraft failed for SR {ServiceRequestId}, provider {ProfileId}",
                request.ServiceRequestId, _identityHolder.ProfileId);
            throw new AizenBusinessException(ExtractMessage(ex.Content) ?? "Failed to save offer draft.");
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
