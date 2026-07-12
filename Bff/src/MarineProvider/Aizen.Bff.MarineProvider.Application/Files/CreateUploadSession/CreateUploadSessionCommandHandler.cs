using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Files.CreateUploadSession;

public sealed class CreateUploadSessionCommandHandler
    : AizenCommandHandler<CreateUploadSessionCommand, CreateUploadSessionBffResponse>
{
    private readonly IProviderFileStorageRemoteCall _fileStorage;
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly ILogger<CreateUploadSessionCommandHandler> _logger;

    public CreateUploadSessionCommandHandler(
        IProviderFileStorageRemoteCall fileStorage,
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        ILogger<CreateUploadSessionCommandHandler> logger)
    {
        _fileStorage = fileStorage;
        _resolver = resolver;
        _identityHolder = identityHolder;
        _logger = logger;
    }

    public override async Task<CreateUploadSessionBffResponse?> Handle(CreateUploadSessionCommand request, CancellationToken ct)
    {
        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module call.
        // FileStorage stamps UploadedByUserId from the asserted user; without this the file is owned by user 0
        // and can never pass the ownership check when it is attached to a profile.
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.UserId is not > 0)
        {
            _logger.LogError("Provider identity unresolved; refusing to create an upload session.");
            return null;
        }

        var category = System.Enum.TryParse<FileCategory>(request.Category, true, out var cat) ? cat : FileCategory.Document;
        var result = await _fileStorage.CreateUploadSession(new CreateUploadSessionRequest
        {
            OriginalFileName = request.FileName,
            ContentType = request.ContentType,
            SizeInBytes = request.Size,
            Category = category,
            Visibility = FileVisibility.Private,
        });

        var data = result.Body;
        if (data is null) return null;

        // Strip BucketName and ObjectKey — never expose to the frontend
        return new CreateUploadSessionBffResponse
        {
            FileId = data.FileId,
            UploadSessionCode = data.UploadSessionCode,
            UploadUrl = data.UploadUrl,
            ExpiresAt = data.ExpiresAt,
            RequiredHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = request.ContentType,
            },
        };
    }
}
