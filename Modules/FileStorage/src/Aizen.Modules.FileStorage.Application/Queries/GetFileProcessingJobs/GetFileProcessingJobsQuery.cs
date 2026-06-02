using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileProcessingJobs;

[DocumentationInfo("Get file processing jobs query", "Returns all background processing jobs for a given file ID.")]
public sealed class GetFileProcessingJobsQuery : AizenQuery<IReadOnlyList<FileProcessingJobDto>>
{
    public long FileId { get; }

    public GetFileProcessingJobsQuery(long fileId)
    {
        FileId = fileId;
    }
}
