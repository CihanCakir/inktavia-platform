using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class CargoDryMarkDeliveredBffCommandHandler
    : AizenCommandHandler<CargoDryMarkDeliveredBffCommand, CargoDryMarkDeliveredBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<CargoDryMarkDeliveredBffCommandHandler> _logger;

    public CargoDryMarkDeliveredBffCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<CargoDryMarkDeliveredBffCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<CargoDryMarkDeliveredBffResponse?> Handle(CargoDryMarkDeliveredBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        if (request.KitId <= 0)
            return new CargoDryMarkDeliveredBffResponse { Success = false, Message = "A delivered kit must be specified." };

        try
        {
            var result = await _serviceRequest.CargoDryMarkDelivered(
                request.ServiceRequestId, new MarkCargoDryDeliveredRemoteRequest { KitId = request.KitId });

            if (result.Header?.IsSuccess != true || result.Body is null)
                return new CargoDryMarkDeliveredBffResponse
                {
                    Success = false,
                    Message = result.Header?.ErrorMessage ?? "Failed to mark the CargoDry order delivered.",
                };

            return new CargoDryMarkDeliveredBffResponse
            {
                Success                 = true,
                ServiceRequestId        = result.Body.ServiceRequestId,
                AutoCompleteDeadlineUtc = result.Body.AutoCompleteDeadlineUtc,
            };
        }
        catch (Refit.ApiException ex)
        {
            var message = ExtractBusinessMessage(ex.Content) ?? "Failed to mark the CargoDry order delivered.";
            _logger.LogWarning(ex, "CargoDry mark-delivered failed for SR {SrId} (status {Status}): {Message}",
                request.ServiceRequestId, ex.StatusCode, message);
            return new CargoDryMarkDeliveredBffResponse { Success = false, Message = message };
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
