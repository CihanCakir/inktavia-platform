using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Consumers.ServiceRequest;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

/// <summary>
/// Wave 4A — provider + admin-feed notifications for provider stock-request transitions. Created → admins (InApp+Email);
/// Approved / Shipped / Rejected → the requesting provider (InApp+Email) + a generic admin activity row in the feed.
/// </summary>
public sealed class CargoDryStockRequestEventConsumer
    : AizenBaseMessageConsumer<CargoDryStockRequestEventMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly INotificationDeepLinkBuilder _deepLinks;
    private readonly ILogger<CargoDryStockRequestEventConsumer> _logger;

    public CargoDryStockRequestEventConsumer(IServiceProvider sp) : base(sp)
    {
        _sender    = sp.GetRequiredService<ISender>();
        _identity  = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _deepLinks = sp.GetRequiredService<INotificationDeepLinkBuilder>();
        _logger    = sp.GetRequiredService<ILogger<CargoDryStockRequestEventConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CargoDryStockRequestEventMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CargoDryStockRequestEventMessage message, CancellationToken ct)
    {
        var id = message.StockRequestId;
        var vars = new Dictionary<string, string>
        {
            { "stockRequestId", id.ToString() },
            { "requestCode",    message.RequestCode ?? id.ToString() },
            { "productCode",    message.ProductCode ?? string.Empty },
            { "quantity",       message.RequestedQuantity?.ToString() ?? string.Empty },
            { "trackingCode",   message.TrackingCode ?? string.Empty },
            { "reason",         message.Reason ?? string.Empty },
        };

        if (message.Event == CargoDryStockRequestEvent.Created)
        {
            // Admin feed (InApp). Admins are resolved as user ids; this codebase's admin notifications are InApp-only
            // (the email dispatcher keys on a participant/provider profile id) — see report note on admin email.
            var createdAdminVars = new Dictionary<string, string>(vars) { ["deepLink"] = _deepLinks.AdminStockRequests() };
            await AdminFeedDispatch.SendToAllAdminsInAppAsync(
                _sender, _identity, _logger, NotificationType.CargoDryStockRequestCreatedAdmin, createdAdminVars,
                "CargoDryStockRequest", id, ct);
            return;
        }

        // Approved / Shipped / Rejected → the requesting provider.
        var (providerType, eventLabel) = message.Event switch
        {
            CargoDryStockRequestEvent.Approved => (NotificationType.CargoDryStockRequestApproved, "Onaylandı"),
            CargoDryStockRequestEvent.Shipped  => (NotificationType.CargoDryStockRequestShipped,  "Kargolandı"),
            CargoDryStockRequestEvent.Rejected => (NotificationType.CargoDryStockRequestRejected, "Reddedildi"),
            _                                  => (NotificationType.CargoDryStockRequestAdminActivity, "Güncellendi"),
        };

        if (message.ProviderProfileId > 0)
        {
            var providerVars = new Dictionary<string, string>(vars) { ["deepLink"] = _deepLinks.ProviderStockRequests() };
            await NotificationChannelDispatch.SendInAppAndEmailAsync(
                _sender, message.ProviderProfileId, providerType, providerVars,
                $"{{\"stockRequestId\":{id}}}", "CargoDryStockRequest", id, ct);
        }

        // Admin feed (generic activity row).
        var adminVars = new Dictionary<string, string>(vars) { ["deepLink"] = _deepLinks.AdminStockRequests(), ["eventLabel"] = eventLabel };
        await AdminFeedDispatch.SendToAllAdminsInAppAsync(
            _sender, _identity, _logger, NotificationType.CargoDryStockRequestAdminActivity, adminVars,
            "CargoDryStockRequest", id, ct);
    }

    public override Task ExecuteRollbackMessage(CargoDryStockRequestEventMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CargoDryStockRequestEventConsumer req={Id} Event={Event}: {Error}",
            message.StockRequestId, message.Event, ex.Message);
        return Task.CompletedTask;
    }
}
