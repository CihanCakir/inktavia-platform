using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Modules.Messaging.Application.Command.SendMessage;

[DocumentationInfo("Send message command", "Carries payload for sending a message within a conversation.")]
public sealed class SendMessageCommand : AizenCommand<SendMessageResponse>
{
    public long ConversationId             { get; }
    public string Content                  { get; }
    public MessageType Type                { get; }
    public bool IsInternalNote             { get; }
    public string? AttachmentFileStorageId { get; }
    public string? AttachmentFileName      { get; }
    public string? AttachmentFileType      { get; }
    public string? UploadSessionCode       { get; }
    public string? Checksum                { get; }
    public string? LocationJson            { get; }

    public SendMessageCommand(
        long conversationId,
        string content,
        MessageType type,
        bool isInternalNote,
        string? attachmentFileStorageId,
        string? attachmentFileName,
        string? attachmentFileType,
        string? uploadSessionCode = null,
        string? checksum = null,
        string? locationJson = null)
    {
        ConversationId          = conversationId;
        Content                 = content;
        Type                    = type;
        IsInternalNote          = isInternalNote;
        AttachmentFileStorageId = attachmentFileStorageId;
        AttachmentFileName      = attachmentFileName;
        AttachmentFileType      = attachmentFileType;
        UploadSessionCode       = uploadSessionCode;
        Checksum                = checksum;
        LocationJson            = locationJson;
    }
}
