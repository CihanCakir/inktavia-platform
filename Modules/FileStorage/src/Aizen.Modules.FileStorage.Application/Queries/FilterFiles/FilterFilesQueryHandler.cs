using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Application.Mapping;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;

namespace Aizen.Modules.FileStorage.Application.Queries.FilterFiles;

[DocumentationInfo("Filter files query handler", "Retrieves files by owner and maps each to FileDto.")]
public sealed class FilterFilesQueryHandler : AizenQueryHandler<FilterFilesQuery, IReadOnlyList<FileDto>>
{
    private readonly IFileRepository _fileRepository;

    public FilterFilesQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public override async Task<IReadOnlyList<FileDto>> Handle(FilterFilesQuery request, CancellationToken cancellationToken)
    {
        var ownerModule = request.Request.OwnerModule ?? string.Empty;
        var ownerEntityType = request.Request.OwnerEntityType ?? string.Empty;
        var ownerEntityId = request.Request.OwnerEntityId ?? Guid.Empty;

        var files = await _fileRepository.GetByOwnerAsync(ownerModule, ownerEntityType, ownerEntityId, cancellationToken);

        return files
            .Where(f =>
                (request.Request.Status == null || f.Status == request.Request.Status) &&
                (request.Request.Category == null || f.Category == request.Request.Category) &&
                (request.Request.Visibility == null || f.Visibility == request.Request.Visibility))
            .Skip(request.Request.PageIndex * request.Request.PageSize)
            .Take(request.Request.PageSize)
            .Select(f => f.ToFileDto())
            .ToList();
    }
}
