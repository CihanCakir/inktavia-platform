using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Files.CompleteUpload;

public sealed class CompleteUploadCommandHandler
    : AizenCommandHandler<CompleteUploadCommand, CompleteUploadBffResponse>
{
    private readonly IProviderFileStorageRemoteCall _fileStorage;
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly ILogger<CompleteUploadCommandHandler> _logger;

    public CompleteUploadCommandHandler(
        IProviderFileStorageRemoteCall fileStorage,
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        ILogger<CompleteUploadCommandHandler> logger)
    {
        _fileStorage = fileStorage;
        _resolver = resolver;
        _identityHolder = identityHolder;
        _logger = logger;
    }

    public override async Task<CompleteUploadBffResponse?> Handle(CompleteUploadCommand request, CancellationToken ct)
    {
        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module call, so
        // FileStorage can check that the session being completed belongs to the calling provider.
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.UserId is not > 0)
        {
            _logger.LogError("Provider identity unresolved; refusing to complete upload session.");
            return new CompleteUploadBffResponse { FileId = request.FileId, Status = "Failed", Message = "Provider identity could not be resolved." };
        }

        var result = await _fileStorage.CompleteUploadSession(request.UploadSessionCode,
            new CompleteUploadSessionRequest());

        var data = result.Body;
        if (data is null) return new CompleteUploadBffResponse { FileId = request.FileId, Status = "Failed", Message = "Upload completion failed." };

        return new CompleteUploadBffResponse
        {
            FileId = data.FileId,
            Status = data.Status.ToString(),
            Message = "Upload completed.",
        };
    }
}
