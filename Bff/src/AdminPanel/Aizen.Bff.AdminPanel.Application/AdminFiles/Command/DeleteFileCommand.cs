using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

public sealed class DeleteFileCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long FileId { get; }
    public string UserToken { get; }
    public DeleteFileCommand(long fileId, string userToken)
    {
        FileId = fileId;
        UserToken = userToken;
    }
}
