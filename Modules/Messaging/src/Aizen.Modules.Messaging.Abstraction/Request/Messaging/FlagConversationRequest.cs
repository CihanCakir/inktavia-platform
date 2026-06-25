namespace Aizen.Modules.Messaging.Abstraction.Request.Messaging;

[DocumentationInfo("Flag conversation request", "Payload for flagging a conversation for review.")]
public sealed record FlagConversationRequest(string Reason);
