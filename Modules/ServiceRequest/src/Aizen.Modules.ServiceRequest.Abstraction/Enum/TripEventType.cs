namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("Trip realtime event type", "Discriminator for the trip realtime frame fanned out to the owner map.")]
public enum TripEventType
{
    Started = 1,
    Location = 2,
    Arrived = 3,
    Cancelled = 4,
}
