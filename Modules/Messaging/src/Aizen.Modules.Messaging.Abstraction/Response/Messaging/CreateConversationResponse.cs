namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

[DocumentationInfo("Create conversation response", "Returns the ID of the created or existing conversation.")]
public sealed class CreateConversationResponse
{
    public string ConversationId { get; }
    public CreateConversationResponse(string conversationId) => ConversationId = conversationId;
}
