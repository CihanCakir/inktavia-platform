using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Consumers.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Constants;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

/// <summary>
/// Wave 4A — owner + assigned-provider + admin-feed notifications when a CARGODRY_SUPPLY order is cancelled. Branches
/// on the (Wave 4A-enriched) category on the message; non-supply cancels are ignored here (handled elsewhere / none).
/// </summary>
public sealed class CargoDrySupplyOrderCancelledNotificationConsumer
    : AizenBaseMessageConsumer<ServiceRequestCancelledMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly INotificationDeepLinkBuilder _deepLinks;
    private readonly ILogger<CargoDrySupplyOrderCancelledNotificationConsumer> _logger;

    public CargoDrySupplyOrderCancelledNotificationConsumer(IServiceProvider sp) : base(sp)
    {
        _sender    = sp.GetRequiredService<ISender>();
        _identity  = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _deepLinks = sp.GetRequiredService<INotificationDeepLinkBuilder>();
        _logger    = sp.GetRequiredService<ILogger<CargoDrySupplyOrderCancelledNotificationConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCancelledMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestCancelledMessage message, CancellationToken ct)
    {
        // Only CARGODRY_SUPPLY cancels are handled here.
        if (!string.Equals(message.ServiceCategoryCode, ServiceRequestServiceCategoryCodes.CargoDrySupply, StringComparison.OrdinalIgnoreCase))
            return;

        var srId = message.ServiceRequestId;
        var baseVars = new Dictionary<string, string>
        {
            { "serviceRequestId", srId.ToString() },
            { "requestCode",      message.RequestCode ?? srId.ToString() },
            { "productCode",      message.CargoDryProductCode ?? string.Empty },
            { "trackingCode",     string.Empty },
            { "eventLabel",       "İptal edildi" },
        };

        // ── Owner ──
        if (message.OwnerUserId > 0)
        {
            var ownerProfileId = await OwnerRecipientResolver.ResolveAsync(_identity, _logger, message.OwnerUserId, ct);
            var ownerVars = new Dictionary<string, string>(baseVars)
            {
                ["deepLink"]       = _deepLinks.OwnerServiceRequest(srId),
                ["webFallbackUrl"] = _deepLinks.OwnerServiceRequestWebFallback(srId),
            };
            await NotificationChannelDispatch.SendInAppAndEmailAsync(
                _sender, ownerProfileId, NotificationType.CargoDrySupplyOrderCancelledOwner, ownerVars,
                $"{{\"serviceRequestId\":{srId}}}", "ServiceRequest", srId, ct);
        }

        // ── Assigned provider (if any) ──
        if (message.AssignedProviderProfileId is { } providerProfileId && providerProfileId > 0)
        {
            var providerVars = new Dictionary<string, string>(baseVars) { ["deepLink"] = _deepLinks.ProviderServiceRequest(srId) };
            await NotificationChannelDispatch.SendInAppAndEmailAsync(
                _sender, providerProfileId, NotificationType.CargoDrySupplyOrderCancelledProvider, providerVars,
                $"{{\"serviceRequestId\":{srId}}}", "ServiceRequest", srId, ct);
        }

        // ── Admin feed ──
        var adminVars = new Dictionary<string, string>(baseVars) { ["deepLink"] = _deepLinks.AdminSupplyOrder(srId) };
        await AdminFeedDispatch.SendToAllAdminsInAppAsync(
            _sender, _identity, _logger, NotificationType.CargoDrySupplyOrderAdminActivity, adminVars,
            "ServiceRequest", srId, ct);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestCancelledMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CargoDrySupplyOrderCancelledNotificationConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
