using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.AdminFiles.Command;

[DocumentationInfo("Bulk generate read URLs command handler", "Generates pre-signed read URLs for multiple files in parallel via the FileStorage module.")]
public sealed class BulkGenerateReadUrlsCommandHandler
    : AizenCommandHandler<BulkGenerateReadUrlsCommand, List<FileAccessUrlResult>>
{
    private readonly IFileStorageAdminBffRemoteCall _fileStorage;

    public BulkGenerateReadUrlsCommandHandler(IFileStorageAdminBffRemoteCall fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public override async Task<List<FileAccessUrlResult>?> Handle(
        BulkGenerateReadUrlsCommand request, CancellationToken cancellationToken)
    {
        var expiresIn = TimeSpan.FromMinutes(request.ExpiresInMinutes);
        var urlRequest = new CreateFileReadUrlRemoteCallRequest { ExpiresIn = expiresIn };

        var tasks = request.FileIds
            .Select(fileId => _fileStorage.CreateReadUrl(fileId, urlRequest, request.Authorization, request.UserToken))
            .ToList();

        await Task.WhenAll(tasks.Select(t => t.ContinueWith(_ => { })));

        return tasks
            .Where(t => t.IsCompletedSuccessfully && t.Result.Body != null)
            .Select(t => t.Result.Body!)
            .ToList();
    }
}
