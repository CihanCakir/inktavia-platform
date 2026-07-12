using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileProcessingJobs;

[DocumentationInfo("Get file processing jobs query handler", "Resolves the file by Guid, then delegates to IFileProcessingService to retrieve all jobs for a file.")]
public sealed class GetFileProcessingJobsQueryHandler : AizenQueryHandler<GetFileProcessingJobsQuery, IReadOnlyList<FileProcessingJobDto>>
{
    private readonly IFileProcessingService _processingService;
    private readonly IFileRepository _fileRepository;

    public GetFileProcessingJobsQueryHandler(IFileProcessingService processingService, IFileRepository fileRepository)
    {
        _processingService = processingService;
        _fileRepository = fileRepository;
    }

    public override async Task<IReadOnlyList<FileProcessingJobDto>> Handle(GetFileProcessingJobsQuery request, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByGuidAsync(request.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File not found: {request.FileId}");

        return await _processingService.GetJobsAsync(file.Id, cancellationToken);
    }
}
