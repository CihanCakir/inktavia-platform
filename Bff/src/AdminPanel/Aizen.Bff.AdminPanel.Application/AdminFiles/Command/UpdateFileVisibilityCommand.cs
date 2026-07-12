using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

public sealed class UpdateFileVisibilityCommand : AizenCommand<EmptyResult>
{
    public Guid FileId { get; }
    public string Visibility { get; }
    public UpdateFileVisibilityCommand(Guid fileId, string visibility)
    {
        FileId = fileId;
        Visibility = visibility;
    }
}
