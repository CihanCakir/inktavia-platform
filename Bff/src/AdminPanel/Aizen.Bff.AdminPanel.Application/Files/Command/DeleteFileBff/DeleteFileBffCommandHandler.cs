using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Files.Command;

[DocumentationInfo("Delete file command handler", "Soft-deletes a file via the FileStorage module admin endpoint.")]
public sealed class DeleteFileBffCommandHandler
    : AizenCommandHandler<DeleteFileBffCommand, AdminBffCommandResultDto>
{
    private readonly IFileStorageRemoteCall _fileStorage;

    public DeleteFileBffCommandHandler(IFileStorageRemoteCall fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        DeleteFileBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _fileStorage.DeleteFile(request.FileId);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "File deletion failed.");
    }
}
