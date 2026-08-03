using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Realtime.MessageConsumers;
using Aizen.Modules.ServiceRequest.Abstraction.Message;

namespace Aizen.Bff.MarineProvider.Realtime;

// One zero-logic closing subclass of the framework's GENERIC RealtimeEventConsumer<TMessage, TResult> per module
// bus event the provider surface consumes. They exist ONLY so the Aizen messagebus consumer scan finds a concrete,
// non-generic consumer type in this BFF entry assembly (open generics aren't auto-closed) and hosts it — the same
// pattern as the admin BFF's AdminMessagingRealtimeConsumer. They carry NO per-event broadcast code: the framework
// consumer forwards the bus message to IRealtimeEventIngress, which applies ProviderEventSocketMapper and
// broadcasts over the shared socket path. (ADR: do not hand-roll per-event consumers.)
//
// These run only because the BFF's AizenAppInfo includes AppType.Worker (enables bus consumption).

/// <summary>Bus → provider hub: a message was added to a conversation involving this provider.</summary>
public sealed class MessageAddedRealtimeConsumer
    : RealtimeEventConsumer<ServiceRequestMessageSentMessage, AizenMessageResult>
{
    public MessageAddedRealtimeConsumer(IServiceProvider sp) : base(sp) { }
}

/// <summary>Bus → provider hub: this provider's offer was accepted.</summary>
public sealed class OfferAcceptedRealtimeConsumer
    : RealtimeEventConsumer<ServiceRequestOfferAcceptedMessage, AizenMessageResult>
{
    public OfferAcceptedRealtimeConsumer(IServiceProvider sp) : base(sp) { }
}

/// <summary>Bus → provider hub: this provider's offer was rejected.</summary>
public sealed class OfferRejectedRealtimeConsumer
    : RealtimeEventConsumer<ServiceRequestOfferRejectedMessage, AizenMessageResult>
{
    public OfferRejectedRealtimeConsumer(IServiceProvider sp) : base(sp) { }
}

/// <summary>Bus → provider hub: a request became biddable in a city.</summary>
public sealed class ServiceRequestPublishedRealtimeConsumer
    : RealtimeEventConsumer<ServiceRequestPublishedMessage, AizenMessageResult>
{
    public ServiceRequestPublishedRealtimeConsumer(IServiceProvider sp) : base(sp) { }
}

/// <summary>Bus → provider hub: a service request's details were updated.</summary>
public sealed class ServiceRequestUpdatedRealtimeConsumer
    : RealtimeEventConsumer<ServiceRequestUpdatedMessage, AizenMessageResult>
{
    public ServiceRequestUpdatedRealtimeConsumer(IServiceProvider sp) : base(sp) { }
}

/// <summary>Bus → provider hub: a service request was cancelled.</summary>
public sealed class ServiceRequestCancelledRealtimeConsumer
    : RealtimeEventConsumer<ServiceRequestCancelledMessage, AizenMessageResult>
{
    public ServiceRequestCancelledRealtimeConsumer(IServiceProvider sp) : base(sp) { }
}

/// <summary>Bus → provider hub: a service request's priority changed.</summary>
public sealed class ServiceRequestUrgencyChangedRealtimeConsumer
    : RealtimeEventConsumer<ServiceRequestUrgencyChangedMessage, AizenMessageResult>
{
    public ServiceRequestUrgencyChangedRealtimeConsumer(IServiceProvider sp) : base(sp) { }
}
