using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class CompositeNotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationDispatcher _inApp;
    private readonly INotificationDispatcher _push;
    private readonly ILogger<CompositeNotificationDispatcher> _logger;

    public CompositeNotificationDispatcher(
        [FromKeyedServices(NotificationChannel.InApp)] INotificationDispatcher inApp,
        [FromKeyedServices(NotificationChannel.Push)]  INotificationDispatcher push,
        ILogger<CompositeNotificationDispatcher> logger)
    {
        _inApp  = inApp;
        _push   = push;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        var dispatcher = notification.Channel switch
        {
            NotificationChannel.InApp => _inApp,
            NotificationChannel.Push  => _push,
            _ => null,
        };

        if (dispatcher is null)
        {
            _logger.LogWarning("No dispatcher registered for channel {Channel}", notification.Channel);
            return;
        }

        await dispatcher.DispatchAsync(notification, ct);
    }
}
