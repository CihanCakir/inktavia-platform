using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

[DocumentationInfo("CreateFileReadUrl command handler", "Generates a pre-signed read URL for a file via the FileStorage module.")]
public sealed class CreateFileReadUrlCommandHandler : AizenCommandHandler<CreateFileReadUrlCommand, FileAccessUrlResult>
{
    private readonly IFileStorageAdminBffRemoteCall _fileStorage;
    public CreateFileReadUrlCommandHandler(IFileStorageAdminBffRemoteCall fileStorage) { _fileStorage = fileStorage; }

    public override async Task<FileAccessUrlResult?> Handle(CreateFileReadUrlCommand request, CancellationToken ct)
    {
        var r = await _fileStorage.CreateReadUrl(
            request.FileId,
            new CreateFileReadUrlRemoteCallRequest { ExpiresIn = TimeSpan.FromMinutes(request.ExpiresInMinutes) },
            request.Authorization,
            request.UserToken);
        return r.Body;
    }
}
