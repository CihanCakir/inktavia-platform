using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

public sealed class BulkGenerateReadUrlsCommand : AizenCommand<List<FileAccessUrlResult>>
{
    public List<long> FileIds { get; }
    public int ExpiresInMinutes { get; }
    public string UserToken { get; }
    public BulkGenerateReadUrlsCommand(List<long> fileIds, int expiresInMinutes, string userToken)
    {
        FileIds = fileIds;
        ExpiresInMinutes = expiresInMinutes;
        UserToken = userToken;
    }
}
