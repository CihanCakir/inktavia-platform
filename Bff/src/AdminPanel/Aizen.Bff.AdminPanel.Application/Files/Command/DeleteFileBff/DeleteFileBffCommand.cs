using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Files.Command;

public sealed class DeleteFileBffCommand : AizenCommand<AdminBffCommandResultDto>
{
    public Guid FileId { get; }
    public DeleteFileBffCommand(Guid fileId)
    {
        FileId = fileId;
    }
}
