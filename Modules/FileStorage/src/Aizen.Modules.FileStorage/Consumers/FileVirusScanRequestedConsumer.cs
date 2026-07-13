using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File virus scan requested consumer", "Fire-and-forget consumer that scans an uploaded file for viruses. Marks the file Ready if clean, Quarantined if a threat is detected.")]
public sealed class FileVirusScanRequestedConsumer : AizenBaseMessageConsumer<FileVirusScanRequestedMessage>
{
    private readonly ILogger<FileVirusScanRequestedConsumer> _logger;
    private readonly IFileScanner _scanner;
    private readonly IFileRepository _fileRepository;
    private readonly FileStorageDbContext _db;
    private readonly IFileCacheInvalidationService _cacheInvalidation;

    public FileVirusScanRequestedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileVirusScanRequestedConsumer>>();
        _scanner = serviceProvider.GetRequiredService<IFileScanner>();
        _fileRepository = serviceProvider.GetRequiredService<IFileRepository>();
        _db = serviceProvider.GetRequiredService<FileStorageDbContext>();
        _cacheInvalidation = serviceProvider.GetRequiredService<IFileCacheInvalidationService>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileVirusScanRequestedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(message.FileId != Guid.Empty);

    public override async Task ExecuteCommitMessage(
        FileVirusScanRequestedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Virus scan starting: FileId={FileId}, ObjectKey={ObjectKey}, Bucket={Bucket}",
            message.FileId, message.ObjectKey, message.BucketName);

        var file = await _fileRepository.GetByGuidAsync(message.FileId, cancellationToken);
        if (file is null)
        {
            _logger.LogWarning("Virus scan: file {FileId} not found. Skipping.", message.FileId);
            return;
        }

        var result = await _scanner.ScanAsync(message.BucketName, message.ObjectKey, cancellationToken);

        if (result.IsClean)
        {
            file.PromoteToReady();
            _logger.LogInformation("Virus scan clean: FileId={FileId} promoted to Ready.", message.FileId);
        }
        else
        {
            file.MarkQuarantined();
            _logger.LogWarning(
                "Virus scan THREAT DETECTED: FileId={FileId}, Threat={ThreatName}. File quarantined.",
                message.FileId, result.ThreatName);
        }

        _fileRepository.Update(file);
        await _db.SaveChangesAsync(cancellationToken);
        await _cacheInvalidation.InvalidateFileAsync(file.Id, file.PublicId ?? Guid.Empty, file.FileCode, cancellationToken);
    }

    public override Task ExecuteRollbackMessage(
        FileVirusScanRequestedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
