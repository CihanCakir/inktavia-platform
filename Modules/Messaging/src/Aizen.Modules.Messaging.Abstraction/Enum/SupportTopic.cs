namespace Aizen.Modules.Messaging.Abstraction.Enum;

/// <summary>
/// N-D live-support reason/topic — set on a <see cref="MessagingContextType.Support"/> conversation (null otherwise).
/// The admin "Destek" queue groups open support requests by this topic. Kept in sync with the FE topic picker.
/// </summary>
public enum SupportTopic
{
    Payment        = 1,
    ServiceRequest = 2,
    Account        = 3,
    Billing        = 4,
    Technical      = 5,
    Other          = 6,
}
