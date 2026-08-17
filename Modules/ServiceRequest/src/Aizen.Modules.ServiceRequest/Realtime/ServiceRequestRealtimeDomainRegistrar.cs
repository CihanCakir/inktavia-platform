using Aizen.Core.Realtime.Abstraction.Domain;
using Aizen.Core.Realtime.Abstraction.Interfaces;

namespace Aizen.Modules.ServiceRequest.Realtime;

[DocumentationInfo("ServiceRequest realtime domain registrar", "Registers all ServiceRequest realtime event names into the global registry at startup.")]
public sealed class ServiceRequestRealtimeDomainRegistrar : IRealtimeDomainRegistrar
{
    public void Register()
    {
        RealtimeEventRegistry.RegisterDomainEvents("servicerequest",
            "ServiceRequestCreated",
            "ServiceRequestUpdated",
            "ServiceRequestStatusChanged",
            "OfferCreated",
            "OfferUpdated",
            "OfferAccepted",
            "OfferRejected",
            "AssignmentCreated",
            "AssignmentUpdated",
            "AssignmentAccepted",
            "AssignmentRejected",
            "MessageSent",
            "WorkLogAdded",
            "WorkStarted",
            "WorkPaused",
            "WorkResumed",
            "CompletionSubmitted",
            "CompletionApproved",
            "CompletionRejected",
            "DisputeOpened",
            "DisputeStatusChanged",
            "DisputeResolved",
            "AdminInterventionRequired");
    }
}
