using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Consumers.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

/// <summary>
/// Wave 4A — owner + admin-feed notifications for CargoDry supply-order transitions (Assigned / Delivered /
/// AwaitingShipment / Shipped). Every event also lands one InApp row in the admin feed. Deep links are per-audience:
/// owner → mobile scheme (+ https fallback in email body); admin → admin web order page.
/// </summary>
public sealed class CargoDrySupplyOrderLifecycleConsumer
    : AizenBaseMessageConsumer<CargoDrySupplyOrderLifecycleMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly INotificationDeepLinkBuilder _deepLinks;
    private readonly ILogger<CargoDrySupplyOrderLifecycleConsumer> _logger;

    public CargoDrySupplyOrderLifecycleConsumer(IServiceProvider sp) : base(sp)
    {
        _sender    = sp.GetRequiredService<ISender>();
        _identity  = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _deepLinks = sp.GetRequiredService<INotificationDeepLinkBuilder>();
        _logger    = sp.GetRequiredService<ILogger<CargoDrySupplyOrderLifecycleConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CargoDrySupplyOrderLifecycleMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CargoDrySupplyOrderLifecycleMessage message, CancellationToken ct)
    {
        var srId = message.ServiceRequestId;
        var (ownerType, eventLabel) = message.Event switch
        {
            CargoDrySupplyOrderLifecycleEvent.Assigned         => (NotificationType.CargoDrySupplyOrderAssignedOwner,         "İş atandı"),
            CargoDrySupplyOrderLifecycleEvent.Delivered        => (NotificationType.CargoDrySupplyOrderDelivered,             "Teslim edildi"),
            CargoDrySupplyOrderLifecycleEvent.AwaitingShipment => (NotificationType.CargoDrySupplyOrderAwaitingShipmentOwner, "Kargoya hazırlanıyor"),
            CargoDrySupplyOrderLifecycleEvent.Shipped          => (NotificationType.CargoDrySupplyOrderShipped,               "Kargolandı"),
            _                                                  => (NotificationType.CargoDrySupplyOrderAdminActivity,        "Güncellendi"),
        };

        // ── Owner ──────────────────────────────────────────────────────────────
        if (message.OwnerUserId > 0)
        {
            var ownerProfileId = await OwnerRecipientResolver.ResolveAsync(_identity, _logger, message.OwnerUserId, ct);
            var ownerVars = BaseVariables(message, eventLabel);
            ownerVars["deepLink"]       = _deepLinks.OwnerServiceRequest(srId);
            ownerVars["webFallbackUrl"] = _deepLinks.OwnerServiceRequestWebFallback(srId);

            await NotificationChannelDispatch.SendInAppAndEmailAsync(
                _sender, ownerProfileId, ownerType, ownerVars,
                $"{{\"serviceRequestId\":{srId}}}", "ServiceRequest", srId, ct);
        }

        // ── Admin feed (every event) ─────────────────────────────────────────────
        var adminVars = BaseVariables(message, eventLabel);
        adminVars["deepLink"] = message.Event == CargoDrySupplyOrderLifecycleEvent.AwaitingShipment
            ? _deepLinks.AdminAwaitingShipment()
            : _deepLinks.AdminSupplyOrder(srId);
        await AdminFeedDispatch.SendToAllAdminsInAppAsync(
            _sender, _identity, _logger, NotificationType.CargoDrySupplyOrderAdminActivity, adminVars,
            "ServiceRequest", srId, ct);
    }

    private static Dictionary<string, string> BaseVariables(CargoDrySupplyOrderLifecycleMessage m, string eventLabel) => new()
    {
        { "serviceRequestId", m.ServiceRequestId.ToString() },
        { "requestCode",      m.RequestCode ?? m.ServiceRequestId.ToString() },
        { "productCode",      m.ProductCode ?? string.Empty },
        { "trackingCode",     m.TrackingCode ?? string.Empty },
        { "eventLabel",       eventLabel },
    };

    public override Task ExecuteRollbackMessage(CargoDrySupplyOrderLifecycleMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CargoDrySupplyOrderLifecycleConsumer SR={SrId} Event={Event}: {Error}",
            message.ServiceRequestId, message.Event, ex.Message);
        return Task.CompletedTask;
    }
}
