using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.PricingAttributes;

/// <summary>S2 — set (full-replace) the pricing attribute values on one offer line. Server validates; failures surface verbatim.</summary>
public sealed class SetOfferLineAttributesBffCommand : AizenCommand<List<PricingAttributeValueDto>>
{
    public long OfferId { get; init; }
    public long ItemId  { get; init; }
    public SetOfferLineAttributesRequest Body { get; init; } = default!;
}

public sealed class SetOfferLineAttributesBffCommandHandler
    : AizenCommandHandler<SetOfferLineAttributesBffCommand, List<PricingAttributeValueDto>>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<SetOfferLineAttributesBffCommandHandler> _logger;

    public SetOfferLineAttributesBffCommandHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest, ILogger<SetOfferLineAttributesBffCommandHandler> logger)
    {
        _resolver = resolver; _identityHolder = identityHolder; _serviceRequest = serviceRequest; _logger = logger;
    }

    public override async Task<List<PricingAttributeValueDto>?> Handle(
        SetOfferLineAttributesBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _serviceRequest.SetOfferLineAttributes(request.OfferId, request.ItemId, request.Body);
            return result.Body ?? new List<PricingAttributeValueDto>();
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "SetOfferLineAttributes failed for offer {OfferId} item {ItemId}", request.OfferId, request.ItemId);
            throw new AizenBusinessException(ExtractMessage(ex.Content) ?? "Failed to save line attributes.");
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
