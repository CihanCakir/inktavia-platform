namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("Trip status", "Live provider-trip lifecycle. Tracking is active ONLY while EnRoute; Arrived/Cancelled are terminal.")]
public enum TripStatus
{
    EnRoute = 1,
    Arrived = 2,
    Cancelled = 3,
}
