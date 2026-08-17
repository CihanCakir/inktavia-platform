using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Application.Mapping;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileById;

[DocumentationInfo("Get file by ID query handler", "Resolves the file by Guid and maps it to FileDto.")]
public sealed class GetFileByIdQueryHandler : AizenQueryHandler<GetFileByIdQuery, FileDto>
{
    private readonly IFileRepository _fileRepository;

    public GetFileByIdQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public override async Task<FileDto> Handle(GetFileByIdQuery request, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByGuidAsync(request.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File not found: {request.FileId}");

        return file.ToFileDto();
    }
}
