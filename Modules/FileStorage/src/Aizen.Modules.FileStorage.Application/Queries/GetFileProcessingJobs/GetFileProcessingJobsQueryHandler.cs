using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileProcessingJobs;

[DocumentationInfo("Get file processing jobs query handler", "Delegates to IFileProcessingService to retrieve all jobs for a file.")]
public sealed class GetFileProcessingJobsQueryHandler : AizenQueryHandler<GetFileProcessingJobsQuery, IReadOnlyList<FileProcessingJobDto>>
{
    private readonly IFileProcessingService _processingService;

    public GetFileProcessingJobsQueryHandler(IFileProcessingService processingService)
    {
        _processingService = processingService;
    }

    public override async Task<IReadOnlyList<FileProcessingJobDto>> Handle(GetFileProcessingJobsQuery request, CancellationToken cancellationToken)
    {
        return await _processingService.GetJobsAsync(request.FileId, cancellationToken);
    }
}
