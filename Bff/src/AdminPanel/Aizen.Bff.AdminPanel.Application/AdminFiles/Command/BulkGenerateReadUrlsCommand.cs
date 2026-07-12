using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

public sealed class BulkGenerateReadUrlsCommand : AizenCommand<List<FileAccessUrlResult>>
{
    public List<Guid> FileIds { get; }
    public int ExpiresInMinutes { get; }
    public BulkGenerateReadUrlsCommand(List<Guid> fileIds, int expiresInMinutes)
    {
        FileIds = fileIds;
        ExpiresInMinutes = expiresInMinutes;
    }
}
