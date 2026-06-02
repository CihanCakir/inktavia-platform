using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Application.Mapping;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFilesByOwner;

[DocumentationInfo("Get files by owner query handler", "Retrieves all files for the specified owner entity and maps each to FileDto.")]
public sealed class GetFilesByOwnerQueryHandler : AizenQueryHandler<GetFilesByOwnerQuery, IReadOnlyList<FileDto>>
{
    private readonly IFileRepository _fileRepository;

    public GetFilesByOwnerQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public override async Task<IReadOnlyList<FileDto>> Handle(GetFilesByOwnerQuery request, CancellationToken cancellationToken)
    {
        var files = await _fileRepository.GetByOwnerAsync(
            request.OwnerModule, request.OwnerEntityType, request.OwnerEntityId, cancellationToken);

        return files.Select(f => f.ToFileDto()).ToList();
    }
}
