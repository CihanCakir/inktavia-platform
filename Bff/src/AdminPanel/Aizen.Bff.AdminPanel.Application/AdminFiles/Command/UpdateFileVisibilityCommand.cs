using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

public sealed class UpdateFileVisibilityCommand : AizenCommand<EmptyResult>
{
    public long FileId { get; }
    public string Visibility { get; }
    public string UserToken { get; }
    public UpdateFileVisibilityCommand(long fileId, string visibility, string userToken)
    {
        FileId = fileId;
        Visibility = visibility;
        UserToken = userToken;
    }
}
