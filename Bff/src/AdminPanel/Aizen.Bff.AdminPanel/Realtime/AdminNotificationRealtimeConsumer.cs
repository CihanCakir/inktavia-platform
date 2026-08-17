using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Realtime.MessageConsumers;
using Aizen.Modules.Notification.Abstraction.Message;

namespace Aizen.Bff.AdminPanel.Realtime;

/// <summary>
/// Registers the framework's GENERIC <see cref="RealtimeEventConsumer{TMessage,TResult}"/> for the Notification
/// module's <see cref="NotificationSentMessage"/> bus event. Zero-logic closing subclass: its only purpose is to be a
/// non-generic type in the BFF entry assembly so the Aizen messagebus consumer scan discovers and hosts it (open
/// generics aren't auto-closed). It adds NO per-event code — the framework consumer forwards the event to
/// <c>IRealtimeEventIngress</c>, which applies <see cref="AdminMessagingEventSocketMapper"/> (the single BFF mapper,
/// broadened to also route notifications) and broadcasts to the recipient's per-user group.
///
/// Runs only because the BFF's AizenAppInfo includes AppType.Worker (enables bus consumption).
/// </summary>
public sealed class AdminNotificationRealtimeConsumer
    : RealtimeEventConsumer<NotificationSentMessage, AizenMessageResult>
{
    public AdminNotificationRealtimeConsumer(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }
}
