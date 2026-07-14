using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// One service request, for the provider.
///
/// The access check lives in the module (the provider must be able to bid on it, have an offer on it, or be
/// assigned to it) — this handler's job is to make sure the module knows *who is asking*. Without
/// <see cref="IProviderProfileResolver.ResolveAsync"/> the identity holder stays empty, the assertion headers are
/// never attached, and the module sees no provider at all.
/// </summary>
public sealed class GetServiceRequestDetailBffQueryHandler
    : AizenQueryHandler<GetServiceRequestDetailBffQuery, GetServiceRequestDetailResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IProviderServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<GetServiceRequestDetailBffQueryHandler> _logger;

    public GetServiceRequestDetailBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IProviderServiceRequestRemoteCall serviceRequest,
        ILogger<GetServiceRequestDetailBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<GetServiceRequestDetailResponse?> Handle(
        GetServiceRequestDetailBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        try
        {
            var result = await _serviceRequest.GetServiceRequestDetail(request.ServiceRequestId);
            return result.Body;
        }
        catch (Refit.ApiException ex)
        {
            // The module answers a forbidden read with the same "not found" it uses for a missing request — telling
            // the caller that a request exists but is not theirs is itself a leak. Pass that through unchanged.
            var message = ExtractBusinessMessage(ex.Content) ?? "Service request not found.";
            _logger.LogWarning(ex, "Service request {ServiceRequestId} not readable by provider {ProfileId}: {Message}",
                request.ServiceRequestId, _identityHolder.ProfileId, message);
            throw new AizenBusinessException(message);
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
            // Not an Aizen envelope — fall through.
        }
        return null;
    }
}
