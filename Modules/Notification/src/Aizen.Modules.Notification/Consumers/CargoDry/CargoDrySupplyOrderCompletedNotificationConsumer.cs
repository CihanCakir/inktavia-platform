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
/// Wave 4A — owner + admin-feed notification when a CargoDry supply order completes (both provider-fulfilled and
/// cargo/direct paths). Reuses the existing <see cref="CargoDrySupplyOrderCompletedMessage"/> published at completion.
/// </summary>
public sealed class CargoDrySupplyOrderCompletedNotificationConsumer
    : AizenBaseMessageConsumer<CargoDrySupplyOrderCompletedMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly INotificationDeepLinkBuilder _deepLinks;
    private readonly ILogger<CargoDrySupplyOrderCompletedNotificationConsumer> _logger;

    public CargoDrySupplyOrderCompletedNotificationConsumer(IServiceProvider sp) : base(sp)
    {
        _sender    = sp.GetRequiredService<ISender>();
        _identity  = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _deepLinks = sp.GetRequiredService<INotificationDeepLinkBuilder>();
        _logger    = sp.GetRequiredService<ILogger<CargoDrySupplyOrderCompletedNotificationConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CargoDrySupplyOrderCompletedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CargoDrySupplyOrderCompletedMessage message, CancellationToken ct)
    {
        var srId = message.ServiceRequestId;
        var vars = new Dictionary<string, string>
        {
            { "serviceRequestId", srId.ToString() },
            { "requestCode",      srId.ToString() },
            { "productCode",      message.ProductCode ?? string.Empty },
            { "trackingCode",     message.TrackingCode ?? string.Empty },
            { "eventLabel",       "Tamamlandı" },
        };

        if (message.OwnerUserId > 0)
        {
            var ownerProfileId = await OwnerRecipientResolver.ResolveAsync(_identity, _logger, message.OwnerUserId, ct);
            var ownerVars = new Dictionary<string, string>(vars)
            {
                ["deepLink"]       = _deepLinks.OwnerServiceRequest(srId),
                ["webFallbackUrl"] = _deepLinks.OwnerServiceRequestWebFallback(srId),
            };
            await NotificationChannelDispatch.SendInAppAndEmailAsync(
                _sender, ownerProfileId, NotificationType.CargoDrySupplyOrderCompleted, ownerVars,
                $"{{\"serviceRequestId\":{srId}}}", "ServiceRequest", srId, ct);
        }

        var adminVars = new Dictionary<string, string>(vars) { ["deepLink"] = _deepLinks.AdminSupplyOrder(srId) };
        await AdminFeedDispatch.SendToAllAdminsInAppAsync(
            _sender, _identity, _logger, NotificationType.CargoDrySupplyOrderAdminActivity, adminVars,
            "ServiceRequest", srId, ct);
    }

    public override Task ExecuteRollbackMessage(CargoDrySupplyOrderCompletedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CargoDrySupplyOrderCompletedNotificationConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
