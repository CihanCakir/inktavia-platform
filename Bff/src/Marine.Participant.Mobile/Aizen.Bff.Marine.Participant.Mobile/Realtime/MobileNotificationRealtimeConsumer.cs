using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Realtime.MessageConsumers;
using Aizen.Modules.Notification.Abstraction.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Realtime;

/// <summary>
/// BE-MO9b — registers the framework's GENERIC <see cref="RealtimeEventConsumer{TMessage,TResult}"/> for the
/// Notification module's <see cref="NotificationSentMessage"/> bus event. Zero-logic closing subclass: its only
/// purpose is to be a non-generic type in the BFF entry assembly so the Aizen messagebus consumer scan discovers and
/// hosts it (open generics aren't auto-closed). It adds NO per-event code — the framework consumer forwards the event
/// to <c>IRealtimeEventIngress</c>, which applies <see cref="MobileNotificationEventSocketMapper"/> and broadcasts to
/// the recipient's per-user group.
///
/// Runs only because the mobile BFF's AizenAppInfo now includes AppType.Worker (enables bus consumption). It is the
/// ONLY bus consumer this BFF hosts — no command consumers are pulled in.
/// </summary>
public sealed class MobileNotificationRealtimeConsumer
    : RealtimeEventConsumer<NotificationSentMessage, AizenMessageResult>
{
    public MobileNotificationRealtimeConsumer(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }
}
