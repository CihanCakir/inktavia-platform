using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Files.Command;

public sealed class UpdateFileVisibilityBffCommand : AizenCommand<EmptyResult>
{
    public Guid FileId { get; }
    public string Visibility { get; }
    public UpdateFileVisibilityBffCommand(Guid fileId, string visibility)
    {
        FileId = fileId;
        Visibility = visibility;
    }
}
