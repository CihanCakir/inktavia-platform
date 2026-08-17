namespace Aizen.Modules.Messaging.Abstraction.Enum;

public enum MessagingRealtimeEventType
{
    MessageSent               = 1,
    MessageModerated          = 2,
    ConversationCreated       = 3,
    ConversationStatusChanged = 4,
    ParticipantJoined         = 5,
    ParticipantLeft           = 6,
    UnreadCountUpdated        = 7,
    AttachmentReady           = 8,
}
