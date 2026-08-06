using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.Files.Command;

public sealed class BulkGenerateReadUrlsBffCommand : AizenCommand<List<FileAccessUrlResult>>
{
    public List<Guid> FileIds { get; }
    public int ExpiresInMinutes { get; }
    public BulkGenerateReadUrlsBffCommand(List<Guid> fileIds, int expiresInMinutes)
    {
        FileIds = fileIds;
        ExpiresInMinutes = expiresInMinutes;
    }
}
