namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

[DocumentationInfo("Create support request response",
    "The support conversation id, whether it was newly created (vs reused), and the topic.")]
public sealed record CreateSupportRequestResponse(string ConversationId, bool Created, string Topic);
