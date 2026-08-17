using Aizen.Core.Realtime.Abstraction.Domain;
using Aizen.Core.Realtime.Abstraction.Interfaces;

namespace Aizen.Modules.Messaging.Realtime;

[DocumentationInfo("Messaging realtime domain registrar",
    "Registers all Messaging module realtime event names into the global registry at startup.")]
public sealed class MessagingRealtimeDomainRegistrar : IRealtimeDomainRegistrar
{
    public void Register()
    {
        RealtimeEventRegistry.RegisterDomainEvents("messaging",
            "MessageReceived",
            "ConversationCreated",
            "ConversationStatusChanged",
            "MessageModerated",
            "UnreadCountUpdated",
            "ParticipantJoined",
            "ParticipantLeft"
        );
    }
}
