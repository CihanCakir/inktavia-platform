using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Realtime.MessageConsumers;
using Aizen.Modules.Messaging.Abstraction.Message;

namespace Aizen.Bff.AdminPanel.Realtime;

/// <summary>
/// Registers the framework's GENERIC <see cref="RealtimeEventConsumer{TMessage,TResult}"/> for the module's
/// <see cref="MessagingMessageSentMessage"/> bus event. This is a zero-logic closing subclass whose only purpose
/// is to be a non-generic type in the BFF entry assembly so the Aizen messagebus consumer scan discovers and
/// hosts it (open generics aren't auto-closed). It adds NO per-event broadcast code — the framework consumer
/// forwards the event to <c>IRealtimeEventIngress</c>, which applies <see cref="AdminMessagingEventSocketMapper"/>
/// and broadcasts via the shared socket path. (ADR: do not hand-roll per-event consumers.)
///
/// Runs only because the BFF's AizenAppInfo includes AppType.Worker (enables bus consumption).
/// </summary>
public sealed class AdminMessagingRealtimeConsumer
    : RealtimeEventConsumer<MessagingMessageSentMessage, AizenMessageResult>
{
    public AdminMessagingRealtimeConsumer(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }
}
