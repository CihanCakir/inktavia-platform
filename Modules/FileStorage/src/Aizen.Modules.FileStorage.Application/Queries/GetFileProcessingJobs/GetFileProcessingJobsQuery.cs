using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileProcessingJobs;

[DocumentationInfo("Get file processing jobs query", "Returns all background processing jobs for a given file ID.")]
public sealed class GetFileProcessingJobsQuery : AizenQuery<IReadOnlyList<FileProcessingJobDto>>
{
    public Guid FileId { get; }

    public GetFileProcessingJobsQuery(Guid fileId)
    {
        FileId = fileId;
    }
}
