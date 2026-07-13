using System.Net;
using System.Text.Json;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class WebPushSender : IPushSender
{
    private readonly WebPushClient _client;
    private readonly VapidDetails _vapidDetails;
    private readonly IUserDeviceTokenRepository _tokenRepository;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(
        IOptions<VapidOptions> options,
        IUserDeviceTokenRepository tokenRepository,
        ILogger<WebPushSender> logger)
    {
        _tokenRepository = tokenRepository;
        _logger = logger;
        var vapid = options.Value;
        _vapidDetails = new VapidDetails(vapid.Subject, vapid.PublicKey, vapid.PrivateKey);
        _client = new WebPushClient();
    }

    public async Task<string> SendAsync(
        UserDeviceTokenEntity subscription,
        string title,
        string body,
        string? dataJson,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(subscription.Endpoint) ||
            string.IsNullOrEmpty(subscription.P256dhKey) ||
            string.IsNullOrEmpty(subscription.AuthKey))
        {
            throw new InvalidOperationException("WebPush subscription is missing required fields.");
        }

        var pushSubscription = new PushSubscription(
            subscription.Endpoint, subscription.P256dhKey, subscription.AuthKey);

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            data = dataJson,
        });

        try
        {
            await _client.SendNotificationAsync(pushSubscription, payload, _vapidDetails, ct);
            return $"webpush:{subscription.Endpoint[..Math.Min(40, subscription.Endpoint.Length)]}";
        }
        catch (WebPushException ex) when (
            ex.StatusCode == HttpStatusCode.NotFound ||
            ex.StatusCode == HttpStatusCode.Gone)
        {
            _logger.LogWarning(
                "WebPush subscription expired ({StatusCode}), deactivating endpoint {Endpoint}.",
                ex.StatusCode, subscription.Endpoint[..Math.Min(40, subscription.Endpoint.Length)]);
            await _tokenRepository.DeactivateAsync(subscription.DeviceToken, ct);
            throw;
        }
    }
}
