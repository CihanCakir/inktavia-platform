using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

public sealed class DeleteFileCommand : AizenCommand<AdminBffCommandResultDto>
{
    public Guid FileId { get; }
    public DeleteFileCommand(Guid fileId)
    {
        FileId = fileId;
    }
}
