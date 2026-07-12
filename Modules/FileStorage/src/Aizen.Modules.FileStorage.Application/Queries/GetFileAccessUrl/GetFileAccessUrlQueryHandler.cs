using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileAccessUrl;

[DocumentationInfo("Get file access URL query handler", "Resolves the file by Guid, then delegates to IFileAccessService to generate the pre-signed read URL.")]
public sealed class GetFileAccessUrlQueryHandler : AizenQueryHandler<GetFileAccessUrlQuery, FileAccessUrlDto>
{
    private readonly IFileAccessService _accessService;
    private readonly IFileRepository _fileRepository;
    private readonly IAizenInfoAccessor _info;

    public GetFileAccessUrlQueryHandler(IFileAccessService accessService, IFileRepository fileRepository, IAizenInfoAccessor info)
    {
        _accessService = accessService;
        _fileRepository = fileRepository;
        _info = info;
    }

    public override async Task<FileAccessUrlDto> Handle(GetFileAccessUrlQuery request, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByGuidAsync(request.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File not found: {request.FileId}");

        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        return await _accessService.CreateReadUrlAsync(file.Id, request.ExpiresIn, userId, cancellationToken);
    }
}
