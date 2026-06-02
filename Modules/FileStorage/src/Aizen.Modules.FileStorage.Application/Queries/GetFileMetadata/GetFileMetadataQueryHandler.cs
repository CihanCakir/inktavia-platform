using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Application.Mapping;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileMetadata;

[DocumentationInfo("Get file metadata query handler", "Loads the file entity and maps it to FileMetadataDto including owner references.")]
public sealed class GetFileMetadataQueryHandler : AizenQueryHandler<GetFileMetadataQuery, FileMetadataDto>
{
    private readonly IFileRepository _fileRepository;

    public GetFileMetadataQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public override async Task<FileMetadataDto> Handle(GetFileMetadataQuery request, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{request.FileId}' not found.");

        return file.ToFileMetadataDto();
    }
}
