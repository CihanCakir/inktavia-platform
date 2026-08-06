using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

[DocumentationInfo("Delete file command handler", "Soft-deletes a file via the FileStorage module admin endpoint.")]
public sealed class DeleteFileCommandHandler
    : AizenCommandHandler<DeleteFileCommand, AdminBffCommandResultDto>
{
    private readonly IFileStorageRemoteCall _fileStorage;

    public DeleteFileCommandHandler(IFileStorageRemoteCall fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        DeleteFileCommand request, CancellationToken cancellationToken)
    {

        var result = await _fileStorage.DeleteFile(request.FileId);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "File deletion failed.");
    }
}
