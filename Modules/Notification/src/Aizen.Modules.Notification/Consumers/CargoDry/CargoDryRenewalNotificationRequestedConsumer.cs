using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

/// <summary>
/// Consumes CargoDryRenewalNotificationRequestedMessage and dispatches per-channel
/// SendNotificationCommand calls for each requested delivery channel.
///
/// Hard rules enforced:
///   #2  — Does NOT call SMS/Mail/Push providers directly.
///   #4  — All channel delivery goes through ISender → SendNotificationCommand.
///   #13 — Idempotency key is passed as MetadataJson for audit and deduplication.
///   #14 — Delivery failure does NOT complete payment.
///   #15 — Delivery failure does NOT renew the kit.
///   #16 — Does NOT create PaymentTransaction.
///
/// Phase 11 (July 2026).
/// </summary>
public sealed class CargoDryRenewalNotificationRequestedConsumer
    : AizenBaseMessageConsumer<CargoDryRenewalNotificationRequestedMessage>
{
    private readonly ISender                                                 _sender;
    private readonly ILogger<CargoDryRenewalNotificationRequestedConsumer>  _logger;

    public CargoDryRenewalNotificationRequestedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<CargoDryRenewalNotificationRequestedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        CargoDryRenewalNotificationRequestedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(
        CargoDryRenewalNotificationRequestedMessage message, CancellationToken ct)
    {
        // ── Guard: owner required to send any notification ─────────────────
        if (message.OwnerUserId is null)
        {
            _logger.LogWarning(
                "CargoDryRenewalNotification skipped: no OwnerUserId. " +
                "RenewalCode={RenewalCode} KitCode={KitCode}",
                message.RenewalCode, message.KitCode);
            return;
        }

        // ── Parse comma-separated channel list ─────────────────────────────
        var channelNames = message.Channels
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // ── Build shared notification variables ────────────────────────────
        var variables = new Dictionary<string, string>
        {
            ["kitCode"]         = message.KitCode,
            ["renewalCode"]     = message.RenewalCode,
            ["productCode"]     = message.ProductCode,
            ["productName"]     = message.ProductName ?? message.ProductCode,
            ["daysUntilExpiry"] = message.DaysUntilExpiry.ToString(),
            ["expiryDate"]      = message.ExpiresAtUtc?.ToString("dd MMM yyyy") ?? "-",
            ["renewalPrice"]    = message.RenewalPrice.HasValue
                ? $"{message.RenewalPrice:F2} {message.CurrencyCode}"
                : "-",
            ["languageCode"]    = message.LanguageCode,
            ["templateCode"]    = message.TemplateCode,
        };

        if (message.RecipientEmail is not null)
            variables["recipientEmail"] = message.RecipientEmail;

        if (message.RecipientPhone is not null)
            variables["recipientPhone"] = message.RecipientPhone;

        // ── Build idempotency metadata ─────────────────────────────────────
        var metadataJson =
            $"{{" +
            $"\"renewalPreparationId\":{message.RenewalPreparationId}," +
            $"\"renewalCode\":\"{message.RenewalCode}\"," +
            $"\"correlationId\":\"{message.CorrelationId}\"," +
            $"\"idempotencyKey\":\"{message.IdempotencyKey}\"," +
            $"\"requestedByUserId\":{message.RequestedByUserId}" +
            $"}}";

        // ── Dispatch per channel ───────────────────────────────────────────
        foreach (var channelName in channelNames)
        {
            if (!Enum.TryParse<NotificationChannel>(channelName, ignoreCase: true, out var channel))
            {
                _logger.LogWarning(
                    "Unknown notification channel '{Channel}' for RenewalCode={RenewalCode}. Skipping.",
                    channelName, message.RenewalCode);
                continue;
            }

            try
            {
                var response = await _sender.Send(new SendNotificationCommand
                {
                    RecipientUserId = message.OwnerUserId.Value,
                    Type            = NotificationType.CargoDryRenewalNotificationRequested,
                    Channel         = channel,
                    Variables       = new Dictionary<string, string>(variables),
                    MetadataJson    = metadataJson,
                    ReferenceType   = "CargoDry",
                    ReferenceId     = message.RenewalPreparationId,
                }, ct);

                _logger.LogInformation(
                    "Renewal notification dispatched. " +
                    "RenewalCode={RenewalCode} Channel={Channel} " +
                    "NotificationId={NotificationId} Dispatched={Dispatched} " +
                    "CorrelationId={CorrelationId}",
                    message.RenewalCode, channel,
                    response.NotificationId, response.Dispatched,
                    message.CorrelationId);
            }
            catch (Exception ex)
            {
                // Log failure but continue with remaining channels.
                // Hard rule #14: delivery failure does NOT complete payment.
                // Hard rule #15: delivery failure does NOT renew the kit.
                _logger.LogError(ex,
                    "Renewal notification delivery FAILED. " +
                    "RenewalCode={RenewalCode} Channel={Channel} CorrelationId={CorrelationId}",
                    message.RenewalCode, channel, message.CorrelationId);
            }
        }
    }

    public override Task ExecuteRollbackMessage(
        CargoDryRenewalNotificationRequestedMessage message,
        AizenMessageError ex,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: CargoDryRenewalNotificationRequestedConsumer. " +
            "RenewalCode={RenewalCode} KitCode={KitCode} Error={Error}",
            message.RenewalCode, message.KitCode, ex.Message);
        return Task.CompletedTask;
    }
}
