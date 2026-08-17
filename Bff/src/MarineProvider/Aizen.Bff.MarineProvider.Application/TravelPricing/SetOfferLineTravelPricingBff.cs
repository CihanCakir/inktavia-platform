using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Travel;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.TravelPricing;

/// <summary>
/// S4 — upsert the structured travel-pricing detail on one Travel offer line. Descriptive only (the server validates the
/// derivation against the line's own money and surfaces SR_TRAVEL_* verbatim); it does not re-price the line.
/// </summary>
public sealed class SetOfferLineTravelPricingBffCommand : AizenCommand<TravelPricingDetailDto>
{
    public long OfferId { get; init; }
    public long ItemId  { get; init; }
    public SetOfferLineTravelPricingRequest Body { get; init; } = default!;
}

public sealed class SetOfferLineTravelPricingBffCommandHandler
    : AizenCommandHandler<SetOfferLineTravelPricingBffCommand, TravelPricingDetailDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<SetOfferLineTravelPricingBffCommandHandler> _logger;

    public SetOfferLineTravelPricingBffCommandHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest, ILogger<SetOfferLineTravelPricingBffCommandHandler> logger)
    {
        _resolver = resolver; _identityHolder = identityHolder; _serviceRequest = serviceRequest; _logger = logger;
    }

    public override async Task<TravelPricingDetailDto?> Handle(
        SetOfferLineTravelPricingBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _serviceRequest.SetOfferLineTravelPricing(request.OfferId, request.ItemId, request.Body);
            return result.Body;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "SetOfferLineTravelPricing failed for offer {OfferId} item {ItemId}", request.OfferId, request.ItemId);
            throw new AizenBusinessException(ExtractMessage(ex.Content) ?? "Failed to save travel pricing.");
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
