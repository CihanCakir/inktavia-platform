namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

[DocumentationInfo("Send message response", "Returns the created message DTO.")]
public sealed class SendMessageResponse
{
    public ChatMessageDto Message { get; }
    public SendMessageResponse(ChatMessageDto message) => Message = message;
}
