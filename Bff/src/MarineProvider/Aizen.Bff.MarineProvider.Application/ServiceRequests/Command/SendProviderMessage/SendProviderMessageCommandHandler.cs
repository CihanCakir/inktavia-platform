using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// Provider sends a free-text message. The module enforces the anti-harassment gate
/// (SR_MSG_CHANNEL_LOCKED when customer hasn't replied). BFF sets SenderType=Provider via assertion.
/// </summary>
public sealed class SendProviderMessageCommandHandler
    : AizenCommandHandler<SendProviderMessageCommand, SendServiceRequestMessageResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<SendProviderMessageCommandHandler> _logger;

    public SendProviderMessageCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<SendProviderMessageCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<SendServiceRequestMessageResponse?> Handle(SendProviderMessageCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _serviceRequest.SendMessage(request.ServiceRequestId,
                new SendServiceRequestMessageRequest
                {
                    Content = request.Content,
                    AttachmentFileId = request.AttachmentFileId,
                    SenderTypeOverride = Aizen.Modules.ServiceRequest.Abstraction.Enum.ServiceRequestMessageSenderType.Provider,
                    LocationLat = request.LocationLat,
                    LocationLng = request.LocationLng,
                    LocationLabel = request.LocationLabel
                });
            return result.Body;
        }
        catch (Refit.ApiException ex)
        {
            var message = ExtractMessage(ex.Content);
            _logger.LogWarning(ex, "SendMessage failed for SR {ServiceRequestId}, provider {ProfileId}: {Message}",
                request.ServiceRequestId, _identityHolder.ProfileId, message);
            throw new AizenBusinessException(message ?? "Failed to send message.");
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
