namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

/// <summary>
/// BE_WC3a — result of the participant-scoped chat-attachment access-check. <see cref="Authorized"/> is true iff the
/// authenticated caller is a participant of the conversation for the given context AND the fileId matches a
/// <c>MessageAttachmentEntity.FileStorageId</c> on one of that conversation's messages (old synced or new native image).
/// The BFF read-url handler tries this first and, on a false/miss, falls back to the SR access-check (request/evidence).
/// </summary>
[DocumentationInfo("Chat attachment access-check response", "Whether the caller may read a chat image via Messaging.")]
public sealed class ChatAttachmentAccessResponse
{
    public bool Authorized { get; init; }

    public ChatAttachmentAccessResponse() { }
    public ChatAttachmentAccessResponse(bool authorized) => Authorized = authorized;
}
