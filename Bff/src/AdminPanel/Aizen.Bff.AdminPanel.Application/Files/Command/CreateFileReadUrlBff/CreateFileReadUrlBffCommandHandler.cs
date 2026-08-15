using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.Files.Command;

[DocumentationInfo("CreateFileReadUrl command handler", "Generates a pre-signed read URL for a file via the FileStorage module.")]
public sealed class CreateFileReadUrlBffCommandHandler : AizenCommandHandler<CreateFileReadUrlBffCommand, FileAccessUrlResult>
{
    private readonly IFileStorageRemoteCall _fileStorage;
    public CreateFileReadUrlBffCommandHandler(
        IFileStorageRemoteCall fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public override async Task<FileAccessUrlResult?> Handle(CreateFileReadUrlBffCommand request, CancellationToken ct)
    {

        var r = await _fileStorage.CreateReadUrl(
            request.FileId,
            new CreateFileReadUrlRemoteCallRequest { ExpiresIn = TimeSpan.FromMinutes(request.ExpiresInMinutes) });
        return new FileAccessUrlResult { AccessUrl = r.Body };
    }
}
