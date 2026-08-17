using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Realtime.MessageConsumers;
using Aizen.Modules.Messaging.Abstraction.Message;

namespace Aizen.Bff.AdminPanel.Realtime;

/// <summary>
/// Zero-logic closing subclass of the framework's generic <see cref="RealtimeEventConsumer{TMessage,TResult}"/> for
/// the module's <see cref="MessagingModerationEventMessage"/> bus event — the moderation sibling of
/// <see cref="AdminMessagingRealtimeConsumer"/>. Exists only so the Aizen messagebus consumer scan discovers a
/// non-generic type in this assembly and hosts it (open generics aren't auto-closed). It adds NO per-event code:
/// the framework consumer forwards to <c>IRealtimeEventIngress</c>, which applies the shared
/// <see cref="AdminMessagingEventSocketMapper"/> and broadcasts. (ADR: do not hand-roll per-event consumers.)
/// </summary>
public sealed class AdminMessagingModerationRealtimeConsumer
    : RealtimeEventConsumer<MessagingModerationEventMessage, AizenMessageResult>
{
    public AdminMessagingModerationRealtimeConsumer(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }
}
