using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Files.Command;

public sealed class CreateFileReadUrlBffCommand : AizenCommand<FileAccessUrlResult>
{
    public Guid FileId { get; }
    public int ExpiresInMinutes { get; }
    public CreateFileReadUrlBffCommand(Guid fileId, int expiresInMinutes)
    {
        FileId = fileId;
        ExpiresInMinutes = expiresInMinutes;
    }
}
