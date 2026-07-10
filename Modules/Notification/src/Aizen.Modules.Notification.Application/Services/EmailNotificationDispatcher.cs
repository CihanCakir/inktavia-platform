using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class EmailNotificationDispatcher : INotificationDispatcher
{
    private readonly IEmailSender _emailSender;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<EmailNotificationDispatcher> _logger;

    public EmailNotificationDispatcher(
        IEmailSender emailSender,
        INotificationRepository notificationRepository,
        ILogger<EmailNotificationDispatcher> logger)
    {
        _emailSender = emailSender;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        // The recipient email is stored in MetadataJson as {"email":"..."} by the handler,
        // or we fall back to a lookup. For password recovery, the consumer sets it.
        string? recipientEmail = null;
        if (!string.IsNullOrWhiteSpace(notification.MetadataJson))
        {
            try
            {
                var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(notification.MetadataJson);
                metadata?.TryGetValue("recipientEmail", out recipientEmail);
            }
            catch { /* metadata is not in expected format, skip */ }
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning(
                "No recipient email found for NotificationId={Id}. Email dispatch skipped.",
                notification.Id);
            return;
        }

        try
        {
            var providerRef = await _emailSender.SendAsync(
                recipientEmail, notification.Title, notification.Body, ct);
            notification.MarkAsSent(providerRef);
            _logger.LogInformation(
                "Email notification dispatched: Id={Id} To={To}",
                notification.Id, recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email dispatch failed for NotificationId={Id}", notification.Id);
            notification.MarkAsFailed();
            await _notificationRepository.UpdateAsync(notification, ct);
        }
    }
}
