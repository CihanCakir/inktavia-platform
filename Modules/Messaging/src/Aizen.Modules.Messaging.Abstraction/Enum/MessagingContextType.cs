namespace Aizen.Modules.Messaging.Abstraction.Enum;

/// <summary>Identifies which domain context owns this conversation.</summary>
public enum MessagingContextType
{
    ServiceRequest  = 1,
    CommerceOrder   = 2,
    CargoDrySupport = 3,
    VenueInquiry    = 4,
    DirectMessage   = 5,
}
