using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Domain.Interface.Service;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Orphan file cleanup consumer", "Fire-and-forget consumer that reaps files with no active owner references older than the configured TTL. Never deletes a file that has an active FileOwnerReference.")]
public sealed class OrphanFileCleanupConsumer : AizenBaseMessageConsumer<OrphanFileCleanupRequestedMessage>
{
    private readonly ILogger<OrphanFileCleanupConsumer> _logger;
    private readonly FileStorageDbContext _db;
    private readonly IObjectStorageProvider _storageProvider;

    public OrphanFileCleanupConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<OrphanFileCleanupConsumer>>();
        _db = serviceProvider.GetRequiredService<FileStorageDbContext>();
        _storageProvider = serviceProvider.GetRequiredService<IObjectStorageProvider>();
    }

    public override Task<bool> ExecutePrepareMessage(
        OrphanFileCleanupRequestedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(message.CleanupTtlHours > 0);

    public override async Task ExecuteCommitMessage(
        OrphanFileCleanupRequestedMessage message, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddHours(-message.CleanupTtlHours);

        // Query for files that:
        //   - Are NOT deleted
        //   - Have NO active owner reference
        //   - Were created before the TTL cutoff
        var orphanedFiles = await _db.Files
            .Where(f => !f.IsDeleted
                && f.Status != FileStatus.Deleted
                && !f.OwnerReferences.Any(r => r.IsActive)
                && f.CreateDate < cutoff)
            .ToListAsync(cancellationToken);

        if (orphanedFiles.Count == 0)
        {
            _logger.LogInformation("Orphan file cleanup: no orphaned files found (TTL={TtlHours}h, cutoff={Cutoff:u}).",
                message.CleanupTtlHours, cutoff);
            return;
        }

        _logger.LogInformation("Orphan file cleanup: found {Count} orphaned file(s) older than {TtlHours}h. Starting cleanup.",
            orphanedFiles.Count, message.CleanupTtlHours);

        var deletedCount = 0;
        foreach (var file in orphanedFiles)
        {
            try
            {
                // Double-check: NEVER delete a file that has an active owner reference.
                // The query already filters, but this is a safety net against race conditions.
                var hasActiveOwner = await _db.FileOwnerReferences
                    .AnyAsync(r => r.FileId == file.Id && r.IsActive, cancellationToken);

                if (hasActiveOwner)
                {
                    _logger.LogWarning(
                        "Orphan cleanup: skipping file {FileId} ({FileCode}) — acquired an owner reference since query.",
                        file.Id, file.FileCode);
                    continue;
                }

                // Delete the S3 object first; if this fails the file stays in DB for retry next cycle
                try
                {
                    await _storageProvider.DeleteObjectAsync(file.BucketName, file.ObjectKey, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Orphan cleanup: failed to delete S3 object for file {FileId} ({ObjectKey}). Will retry next cycle.",
                        file.Id, file.ObjectKey);
                    continue;
                }

                // Soft-delete the file record
                file.SoftDelete(deletedByUserId: null);
                _db.Files.Update(file);
                deletedCount++;

                _logger.LogInformation(
                    "Orphan cleanup: soft-deleted file {FileId} ({FileCode}), ObjectKey={ObjectKey}, Bucket={Bucket}, CreatedAt={CreatedAt:u}.",
                    file.Id, file.FileCode, file.ObjectKey, file.BucketName, file.CreateDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orphan cleanup: unexpected error processing file {FileId} ({FileCode}).",
                    file.Id, file.FileCode);
            }
        }

        if (deletedCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Orphan file cleanup complete: {DeletedCount}/{TotalCount} file(s) cleaned up.",
                deletedCount, orphanedFiles.Count);
        }
        else
        {
            _logger.LogInformation("Orphan file cleanup complete: no files were actually deleted.");
        }

        // TODO: Phase 2 — Phantom claim detection (second pass).
        // Files that ARE claimed (have an active FileOwnerReference) but where the owner entity
        // no longer exists are "phantom claims" — the file is immortal because the sweep skips
        // claimed files, yet the owning document was never persisted or was rolled back.
        // For Identity documents the OwnerEntityType is "OrganizerVerificationDocument" and
        // OwnerEntityId is a Guid that should map to a VerificationDocumentEntity.PublicId.
        // Since FileStorage cannot query Identity's tables, this check requires either:
        //   (a) a cross-module RPC to validate owner existence, or
        //   (b) an async verification message that Identity's module responds to.
        // The primary defence against phantom claims is the source-side fix in
        // FileAttachmentValidationService (persist first, claim second). This TODO is the
        // secondary defence layer.
    }

    public override Task ExecuteRollbackMessage(
        OrphanFileCleanupRequestedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
