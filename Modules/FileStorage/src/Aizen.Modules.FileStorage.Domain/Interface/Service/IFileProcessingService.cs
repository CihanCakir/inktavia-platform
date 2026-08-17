using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.Processing;

namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("File processing service interface", "Creates and manages background processing jobs for file post-processing.")]
public interface IFileProcessingService
{
    Task<FileProcessingJobDto> StartProcessingAsync(long fileId, FileProcessingType processingType, CancellationToken cancellationToken = default);
    Task<bool> UpdateResultAsync(long fileId, UpdateFileProcessingResultRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileProcessingJobDto>> GetJobsAsync(long fileId, CancellationToken cancellationToken = default);
}
