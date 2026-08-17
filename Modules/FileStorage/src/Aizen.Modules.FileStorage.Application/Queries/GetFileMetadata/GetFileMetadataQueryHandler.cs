using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Application.Mapping;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileMetadata;

[DocumentationInfo("Get file metadata query handler", "Resolves the file by Guid and maps it to FileMetadataDto including owner references.")]
public sealed class GetFileMetadataQueryHandler : AizenQueryHandler<GetFileMetadataQuery, FileMetadataDto>
{
    private readonly IFileRepository _fileRepository;

    public GetFileMetadataQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public override async Task<FileMetadataDto> Handle(GetFileMetadataQuery request, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByGuidAsync(request.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File not found: {request.FileId}");

        return file.ToFileMetadataDto();
    }
}
