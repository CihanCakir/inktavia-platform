using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

public sealed class CreateFileReadUrlCommand : AizenCommand<FileAccessUrlResult>
{
    public long FileId { get; }
    public int ExpiresInMinutes { get; }
    public string UserToken { get; }
    public CreateFileReadUrlCommand(long fileId, int expiresInMinutes, string userToken)
    {
        FileId = fileId;
        ExpiresInMinutes = expiresInMinutes;
        UserToken = userToken;
    }
}
