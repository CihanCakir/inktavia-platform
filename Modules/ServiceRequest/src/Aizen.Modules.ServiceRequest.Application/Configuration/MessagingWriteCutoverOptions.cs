namespace Aizen.Modules.ServiceRequest.Application.Configuration;

/// <summary>
/// BE_WC1 — the ServiceRequest host's view of the Phase-4 write-cutover flags (config section
/// <c>Messaging:WriteCutover</c>, the canonical cutover namespace shared with the Messaging host). Only
/// <see cref="SystemMessages"/> is consumed here; the rest are declared for parity/visibility. Default OFF ⇒ the SR
/// commands keep writing their <c>sr.Messages</c> System/offer rows + publishing the chat-mirror event (prior
/// behaviour). When ON, the SR commands stop doing both and the System/offer messages come ONLY from the Messaging
/// lifecycle consumers (driven by the first-class SR domain events, which are always published). Reversible: flip off
/// and the SR path resumes.
/// </summary>
public sealed class MessagingWriteCutoverOptions
{
    public const string SectionName = "Messaging:WriteCutover";

    /// <summary>WC1 — when true, the 5 SR lifecycle commands stop writing <c>sr.Messages</c> System/offer rows and stop
    /// publishing the System/offer <c>ServiceRequestMessageSentMessage</c>; Messaging generates them instead.</summary>
    public bool SystemMessages { get; set; }

    public bool WriteMessagesToMessaging { get; set; }
    public bool ReadMessagesFromMessaging { get; set; }
    public bool DisableServiceRequestMessageSync { get; set; }

    /// <summary>BE_WC2 — participant chat WRITE flip, consumed by the BFFs (owner + provider): TEXT + LOCATION sends go
    /// to the Messaging store when ON (images stay on the SR path until WC3). Declared for the shared flag inventory;
    /// the SR module does not read it. Default OFF, reversible.</summary>
    public bool ChatMessages { get; set; }
}
