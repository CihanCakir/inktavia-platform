using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.CreateReadUrl;

[DocumentationInfo("Create read URL command handler", "Resolves the file by Guid, then delegates to IFileAccessService to generate a pre-signed S3 read URL.")]
public sealed class CreateReadUrlCommandHandler : AizenCommandHandler<CreateReadUrlCommand, FileAccessUrlDto>
{
    private readonly IFileAccessService _accessService;
    private readonly IFileRepository _fileRepository;
    private readonly IAizenInfoAccessor _info;

    public CreateReadUrlCommandHandler(IFileAccessService accessService, IFileRepository fileRepository, IAizenInfoAccessor info)
    {
        _accessService = accessService;
        _fileRepository = fileRepository;
        _info = info;
    }

    public override async Task<FileAccessUrlDto?> Handle(CreateReadUrlCommand command, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByGuidAsync(command.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File not found: {command.FileId}");

        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var expiresIn = command.Request?.ExpiresIn ?? TimeSpan.FromMinutes(15);
        return await _accessService.CreateReadUrlAsync(file.Id, expiresIn, userId, cancellationToken);
    }
}
