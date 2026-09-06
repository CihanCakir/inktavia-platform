using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Realtime.MessageConsumers;
using Aizen.Modules.ServiceRequest.Abstraction.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Realtime;

/// <summary>
/// Phase-2 — registers the framework's GENERIC <see cref="RealtimeEventConsumer{TMessage,TResult}"/> for the SR
/// module's <see cref="TripRealtimeMessage"/> bus event. Zero-logic closing subclass (open generics aren't
/// auto-closed by the messagebus scanner). The framework consumer forwards the event to <c>IRealtimeEventIngress</c>,
/// which applies <see cref="MobileNotificationEventSocketMapper"/> (extended to map trip events) and broadcasts to the
/// per-SR group <c>trip:{serviceRequestId}</c>.
/// </summary>
public sealed class MobileTripRealtimeConsumer
    : RealtimeEventConsumer<TripRealtimeMessage, AizenMessageResult>
{
    public MobileTripRealtimeConsumer(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }
}
