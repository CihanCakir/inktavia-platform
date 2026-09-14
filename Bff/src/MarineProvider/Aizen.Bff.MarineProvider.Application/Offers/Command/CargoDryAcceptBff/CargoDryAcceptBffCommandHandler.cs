using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class CargoDryAcceptBffCommandHandler
    : AizenCommandHandler<CargoDryAcceptBffCommand, CargoDryAcceptBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<CargoDryAcceptBffCommandHandler> _logger;

    public CargoDryAcceptBffCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<CargoDryAcceptBffCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<CargoDryAcceptBffResponse?> Handle(CargoDryAcceptBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _serviceRequest.CargoDryAccept(request.ServiceRequestId);

            if (result.Header?.IsSuccess != true || result.Body is null)
                return new CargoDryAcceptBffResponse
                {
                    Success = false,
                    Message = result.Header?.ErrorMessage ?? "Failed to accept CargoDry supply request.",
                };

            return new CargoDryAcceptBffResponse
            {
                Success          = true,
                OfferId          = result.Body.OfferId,
                ServiceRequestId = result.Body.ServiceRequestId,
                RetailPrice      = result.Body.RetailPrice,
                CurrencyCode     = result.Body.CurrencyCode,
            };
        }
        catch (Refit.ApiException ex)
        {
            // The SR module surfaces gate failures (SR_CARGODRY_NOT_PROGRAM_PROVIDER / SR_CARGODRY_PRODUCT_UNAVAILABLE)
            // and other business errors as a non-2xx envelope. Return a clean message, never a 500/transport leak.
            var message = ExtractBusinessMessage(ex.Content) ?? "Failed to accept CargoDry supply request.";
            _logger.LogWarning(ex, "CargoDry accept failed for SR {SrId} (status {Status}): {Message}",
                request.ServiceRequestId, ex.StatusCode, message);
            return new CargoDryAcceptBffResponse { Success = false, Message = message };
        }
    }

    private static string? ExtractBusinessMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("header", out var header)
                && header.TryGetProperty("errorMessage", out var msg)
                && msg.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = msg.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Not an Aizen envelope
        }
        return null;
    }
}
