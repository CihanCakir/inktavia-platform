using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

public sealed class CreateFileReadUrlCommand : AizenCommand<FileAccessUrlResult>
{
    public Guid FileId { get; }
    public int ExpiresInMinutes { get; }
    public CreateFileReadUrlCommand(Guid fileId, int expiresInMinutes)
    {
        FileId = fileId;
        ExpiresInMinutes = expiresInMinutes;
    }
}
